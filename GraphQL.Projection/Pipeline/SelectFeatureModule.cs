using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQLParser.AST;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public delegate Expression PipelineStep(
        GraphQLField field,
        Expression parameter,
        Type currentType,
        PipelineContext context);

    public sealed record PipelineContext(PipelineStep Next, PipelineStep Pipeline);

    public static PipelineStep CreatePipeline(params PipelineStep[] processors)
    {
        return processors.Reverse().Aggregate((first, second) =>
        {
            return (field, param, type, context) =>
            {
                return second(field, param, type, context with { Next = first });
            };
        });
    }

    public readonly static PipelineStep PrimitiveProcessor = (field, parameter, type, context) =>
    {
        var fieldName = field.Name.StringValue;
        var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
            throw new NullReferenceException();

        if (property.PropertyType.IsPrimitive())
            return Expression.MakeMemberAccess(parameter, property);

        return context.Next(field, parameter, type, context);
    };

    public readonly static PipelineStep SubEntityProcessor = (field, parameter, type, context) =>
    {
        var fieldName = field.Name.StringValue;
        var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
            throw new NullReferenceException();

        if (property.PropertyType.IsPrimitive() || property.PropertyType.IsEnumerable())
            return context.Next(field, parameter, type, context);

        var subEntityParam = Expression.MakeMemberAccess(parameter, property);

        // Process the sub-entity selection set with a NEW context that starts from the BEGINNING
        var nestedContext = new PipelineContext(context.Pipeline, context.Pipeline);
        return ProcessSelectionSet(field.SelectionSet, subEntityParam, property.PropertyType, nestedContext);
    };

    public readonly static PipelineStep CollectionProcessor = (field, parameter, type, context) =>
    {
        var fieldName = field.Name.StringValue;
        var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
            throw new NullReferenceException();

        var propType = property.PropertyType;
        if (propType.IsEnumerable())
        {
            var subEntityType = propType.GenericTypeArguments.FirstOrDefault();
            var childParameter = Expression.Parameter(subEntityType);

            // Process collection elements with a NEW context
            var nestedContext = new PipelineContext(context.Pipeline, context.Pipeline);
            var childMemberInit = ProcessSelectionSet(field.SelectionSet, childParameter, subEntityType, nestedContext);

            var selectMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .MakeGenericMethod(subEntityType, childMemberInit.Type);

            var selectorLambda = Expression.Lambda(childMemberInit, childParameter);
            var selectCall = Expression.Call(selectMethod, Expression.PropertyOrField(parameter, property.Name), selectorLambda);

            return selectCall;
        }

        return context.Next(field, parameter, type, context);
    };

    public readonly static PipelineStep TerminalProcessor = (field, parameter, type, context) =>
    {
        return Expression.Empty();
    };

    public static PipelineStep CreateDefaultPipeline() => CreatePipeline(
        PrimitiveProcessor,
        SubEntityProcessor,
        CollectionProcessor,
        TerminalProcessor);

    public static GraphQLFeatureModule<TEntity> Create<TEntity>()
    {
        var parameter = Expression.Parameter(typeof(TEntity));
        var pipeline = CreateDefaultPipeline();
        var context = new PipelineContext(pipeline, pipeline);
        return (selectionSet, model) =>
        {
            var result = ProcessSelectionSet(selectionSet, parameter, typeof(TEntity), context);
            var expression = Expression.Lambda<Func<TEntity, TEntity>>(result, parameter);
            return model with
            {
                Select = expression
            };

            //var parameter = Expression.Parameter(typeof(TEntity));
            //var bindings = BuildAssignements(typeof(TEntity), parameter, selectionSet);
            //var result = MemberInit(typeof(TEntity), bindings);
            //var expression = Expression.Lambda<Func<TEntity, TEntity>>(result, parameter);
            //return model with
            //{
            //    Select = expression
            //};
        };
    }

    private static Expression ProcessSelectionSet(
        GraphQLSelectionSet selectionSet,
        Expression parameter,
        Type type,
        PipelineContext context)
    {
        var bindings = new List<MemberBinding>();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is GraphQLField field)
            {
                var result = ProcessField(field, parameter, type, context);
                var property = type.GetProperty(field.Name.StringValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new NullReferenceException();

                bindings.Add(Expression.Bind(property, result));
            }
        }

        return Expression.MemberInit(Expression.New(type), bindings);
    }

    private static Expression ProcessField(GraphQLField field, Expression parameter, Type currentType, PipelineContext context) =>
        context.Next(field, parameter, currentType, context);

    private static MemberInitExpression MemberInit(Type type, IEnumerable<MemberAssignment> assignments)
    {
        return Expression.MemberInit(Expression.New(type), assignments);
    }

    private static List<MemberAssignment> BuildAssignements(Type type, Expression parameter, GraphQLSelectionSet set)
    {
        var bindings = new List<MemberAssignment>(set.Selections.Count);

        AssignMember(type, parameter, bindings, set, 0);

        return bindings;
    }

    private static void AssignMember(Type type, Expression parameter, List<MemberAssignment> result, GraphQLSelectionSet set, int index)
    {
        if (index >= set.Selections.Count)
            return;

        var member = set.Selections[index];
        if (member is GraphQLField field)
        {
            var assignement = Assign(type, parameter, field);
            result.Add(assignement);
        }

        AssignMember(type, parameter, result, set, index + 1);
    }

    private static MemberAssignment Assign(Type type, Expression parameter, GraphQLField field)
    {
        var fieldName = field.Name.StringValue;
        var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        ArgumentNullException.ThrowIfNull(property);

        var propType = property.PropertyType;

        if (propType.IsPrimitive())
        {
            var memberAccess = Expression.MakeMemberAccess(parameter, property);
            return Expression.Bind(property, memberAccess);
        }

        var subParameter = Expression.Property(parameter, property);

        if (propType.IsEnumerable())
        {
            var subEntityType = propType.GenericTypeArguments.FirstOrDefault();
            var childParameter = Expression.Parameter(subEntityType, "email");
            var childAssignements = BuildAssignements(subEntityType, childParameter, field.SelectionSet);
            var childMemberInit = MemberInit(subEntityType, childAssignements);

            var selectMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .MakeGenericMethod(subEntityType, childMemberInit.Type);

            var selectorLambda = Expression.Lambda(
                childMemberInit,
                childParameter
            );

            var selectCall = Expression.Call(
                selectMethod,
                Expression.PropertyOrField(parameter, property.Name), // source collection
                selectorLambda // selector
            );

            return Expression.Bind(property, selectCall);
        }

        var assignements = BuildAssignements(propType, subParameter, field.SelectionSet);
        var memberInit = MemberInit(propType, assignements);
        return Expression.Bind(property, memberInit);
    }
}