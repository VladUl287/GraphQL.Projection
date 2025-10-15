using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQLParser.AST;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public delegate Expression PipelineStep(GraphQLField Field, Context Context);
    public delegate PipelineStep PipelineComposer(PipelineStep next);

    public sealed record Context(Expression Parameter, Type Type, PipelineStep Pipeline);

    public static PipelineStep Compose(PipelineStep terminal, PipelineComposer[] composers)
    {
        return composers.Aggregate(terminal, (current, composer) => composer(current));
    }

    public static PipelineStep CreateDefaultPipeline() => Compose(TerminalStep, [CollectionComposer, EntityComposer, PrimitiveComposer]);

    public static GraphQLFeatureModule Create(Type type, PipelineStep pipeline)
    {
        var parameter = Expression.Parameter(type);
        var lambdaType = typeof(Func<,>).MakeGenericType(type, type);
        return (selectionSet, model) =>
        {
            var memberInit = BuildMemberInit(selectionSet, parameter, type, pipeline);
            var expression = Expression.Lambda(lambdaType, memberInit, parameter);
            return model with
            {
                Select = expression
            };
        };
    }

    private static MemberInitExpression BuildMemberInit(
        GraphQLSelectionSet selectionSet,
        Expression parameter,
        Type type,
        PipelineStep pipeline)
    {
        var bindings = selectionSet.Selections
            .OfType<GraphQLField>()
            .Select(field => ProcessField(field, parameter, type, pipeline))
            .Aggregate(
                new List<MemberBinding>(),
                (acc, fieldBind) =>
                {
                    acc.Add(fieldBind);
                    return acc;
                }
            );
        return Expression.MemberInit(Expression.New(type), bindings);
    }

    private static MemberAssignment ProcessField(
        GraphQLField field,
        Expression parameter,
        Type type,
        PipelineStep pipeline)
    {
        var property = type.GetProperty(field.Name.StringValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new NullReferenceException();

        var context = new Context(parameter, type, pipeline);
        var propertyExpression = pipeline(field, context);
        return Expression.Bind(property, propertyExpression);
    }

    public readonly static PipelineComposer PrimitiveComposer = (next) => (field, context) =>
    {
        var fieldName = field.Name.StringValue;
        var property = context.Type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
            throw new NullReferenceException();

        if (property.PropertyType.IsPrimitive())
            return Expression.MakeMemberAccess(context.Parameter, property);

        return next(field, context);
    };

    public readonly static PipelineComposer EntityComposer = (next) => (field, context) =>
    {
        var fieldName = field.Name.StringValue;
        var property = context.Type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
            throw new NullReferenceException();

        if (property.PropertyType.IsPrimitive() || property.PropertyType.IsEnumerable())
            return next(field, context);

        var childParameter = Expression.MakeMemberAccess(context.Parameter, property);
        var memberInit = BuildMemberInit(field.SelectionSet, childParameter, property.PropertyType, context.Pipeline);
        return memberInit;
    };

    public readonly static PipelineComposer CollectionComposer = (next) => (field, context) =>
    {
        var fieldName = field.Name.StringValue;
        var property = context.Type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
            throw new NullReferenceException();

        var propType = property.PropertyType;
        if (propType.IsEnumerable())
        {
            var subEntityType = propType.GenericTypeArguments.FirstOrDefault();
            var childParameter = Expression.Parameter(subEntityType);

            var memberInit = BuildMemberInit(field.SelectionSet, childParameter, subEntityType, context.Pipeline);

            var selectMethod = typeof(Enumerable).GetMethods()
                .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
                .MakeGenericMethod(subEntityType, memberInit.Type);

            var selectorLambda = Expression.Lambda(memberInit, childParameter);
            var selectCall = Expression.Call(selectMethod, Expression.PropertyOrField(context.Parameter, property.Name), selectorLambda);

            return selectCall;
        }

        return next(field, context);
    };

    public readonly static PipelineStep TerminalStep = (field, context) =>
    {
        return Expression.Empty();
    };
}