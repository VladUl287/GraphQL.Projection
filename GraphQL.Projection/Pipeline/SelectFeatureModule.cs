using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQLParser.AST;
using LanguageExt;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public delegate Expression PipelineStep(GraphQLField Field, Context Context);
    public delegate PipelineStep PipelineComposer(PipelineStep next);

    public delegate MemberInitExpression TreeProcessor(GraphQLSelectionSet selectionSet, Expression sourceParameter, Type targetType, PipelineStep pipeline);

    public sealed record Context(Expression Parameter, Type Type, PipelineStep Pipeline, TreeProcessor ProcessTree);

    public static PipelineStep Compose(PipelineStep terminal, PipelineComposer[] composers) =>
        composers.Aggregate(terminal, (current, nextComposer) => nextComposer(current));

    public static PipelineStep CreateDefaultPipeline() => Compose(TerminalStep, [CollectionComposer, EntityComposer, PrimitiveComposer, WithArguments]);

    public static GraphQLFeatureModule Create(Type type, PipelineStep pipeline)
    {
        var parameter = Expression.Parameter(type);
        var genericLambda = typeof(Func<,>).MakeGenericType(type, type);
        return (set, model) =>
        {
            var memberInit = ProcessTree(set, parameter, type, pipeline);
            var expression = Expression.Lambda(genericLambda, memberInit, parameter);
            return model with
            {
                Select = expression
            };
        };
    }

    private static MemberInitExpression ProcessTree(
        GraphQLSelectionSet selectionSet,
        Expression sourceParameter,
        Type targetType,
        PipelineStep pipeline)
    {
        var context = new Context(sourceParameter, targetType, pipeline, ProcessTree);
        var fieldBindings = CreateFieldBindings(selectionSet, context);
        var constructorCall = Expression.New(targetType);
        return Expression.MemberInit(constructorCall, fieldBindings);
    }

    private static IEnumerable<MemberBinding> CreateFieldBindings(
        GraphQLSelectionSet selectionSet,
        Context context)
    {
        return selectionSet.Selections
            .OfType<GraphQLField>()
            .Select(field => CreateFieldBinding(field, context));
    }

    private static MemberAssignment CreateFieldBinding(
        GraphQLField field,
        Context context)
    {
        var property = context.Type.GetProperty(field.Name.StringValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
            ?? throw new NullReferenceException();

        var propertyExpression = context.Pipeline(field, context);
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

    private readonly static PipelineComposer WithArguments = (next) => (field, context) =>
    {
        var result = next(field, context);

        if (field.Arguments is { Count: > 0 })
        {
            ApplyArguments(result, field.Arguments[0], typeof(object));
        }

        return result;
    };

    private static Expression ApplyArguments(Expression source, GraphQLArgument arg, Type type)
    {
        return arg.Name.StringValue switch
        {
            "where" => ApplyWhereFilter(source, arg, type),
            _ => source
        };
    }

    private static Expression ApplyWhereFilter(Expression expression, GraphQLArgument arg, Type type)
    {
        return arg switch
        {
            _ => expression
        };
    }

    public readonly static PipelineComposer EntityComposer = (next) => (field, context) =>
    {
        var fieldName = field.Name.StringValue;
        var property = context.Type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
            throw new NullReferenceException();

        if (field is not { SelectionSet.Selections.Count: > 0 })
            return next(field, context);

        if (property.PropertyType.IsEnumerable())
            return next(field, context);

        var childParameter = Expression.MakeMemberAccess(context.Parameter, property);
        var memberInit = context.ProcessTree(field.SelectionSet, childParameter, property.PropertyType, context.Pipeline);
        return memberInit;
    };

    public readonly static PipelineComposer CollectionComposer = (next) => (field, context) =>
    {
        var fieldName = field.Name.StringValue;
        var property = context.Type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
            throw new NullReferenceException();

        var propType = property.PropertyType;

        if (field is not { SelectionSet.Selections.Count: > 0 })
            return next(field, context);

        if (!propType.IsEnumerable())
            return next(field, context);

        var subEntityType = propType.GenericTypeArguments.FirstOrDefault();
        var childParameter = Expression.Parameter(subEntityType);

        var memberInit = context.ProcessTree(field.SelectionSet, childParameter, subEntityType, context.Pipeline);

        var selectMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
            .MakeGenericMethod(subEntityType, memberInit.Type);

        var selectorLambda = Expression.Lambda(memberInit, childParameter);
        var selectCall = Expression.Call(selectMethod, Expression.PropertyOrField(context.Parameter, property.Name), selectorLambda);

        return selectCall;
    };

    public readonly static PipelineStep TerminalStep = (field, context) =>
    {
        return Expression.Empty();
    };
}