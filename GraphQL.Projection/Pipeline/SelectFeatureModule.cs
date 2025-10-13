using GraphQL.Projection.Models;
using GraphQLParser.AST;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public delegate Expression PipelineStep(GraphQLField Field, Context Context);
    public delegate PipelineStep PipelineComposer(PipelineStep next);

    public sealed record Context(Expression Parameter, Type Type, PipelineStep Next);

    public static PipelineStep Compose(PipelineStep terminal, PipelineComposer[] composers)
    {
        return composers.Aggregate(terminal, (current, composer) => composer(current));
    }

    public static PipelineStep CreateDefault() => Compose(TerminalStep, [CollectionComposer, EntityComposer, PrimitiveComposer]);

    public static GraphQLFeatureModule<TEntity> Create<TEntity>(Func<PipelineStep>? pipelineFactory = null)
    {
        var pipeline = pipelineFactory?.Invoke() ?? CreateDefault();
        var parameter = Expression.Parameter(typeof(TEntity));
        return (selectionSet, model) =>
        {
            var memberInit = BuildMemberInit(selectionSet, parameter, typeof(TEntity), pipeline);
            var expression = Expression.Lambda<Func<TEntity, TEntity>>(memberInit, parameter);
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

        var context = new Context(parameter, type, TerminalStep);
        var propertyExpression = pipeline(field, context);
        return Expression.Bind(property, propertyExpression);
    }

    public static PipelineComposer PrimitiveComposer = (next) => (field, context) =>
    {
        return next(field, context);
    };

    public static PipelineComposer EntityComposer = (next) => (field, context) =>
    {
        return next(field, context);
    };

    public static PipelineComposer CollectionComposer = (next) => (field, context) =>
    {
        var arguments = field.Arguments;

        return next(field, context);
    };

    public readonly static PipelineStep TerminalStep = (field, context) =>
    {
        return Expression.Empty();
    };

    //public readonly static PipelineDelegate PrimitiveProcessor = (field, parameter, type, context) =>
    //{
    //    var fieldName = field.Name.StringValue;
    //    var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
    //        throw new NullReferenceException();

    //    if (property.PropertyType.IsPrimitive())
    //        return Expression.MakeMemberAccess(parameter, property);

    //    return context(field, parameter, type, context);
    //};

    //public readonly static PipelineDelegate SubEntityProcessor = (field, parameter, type, next) =>
    //{
    //    var fieldName = field.Name.StringValue;
    //    var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
    //        throw new NullReferenceException();

    //    if (property.PropertyType.IsPrimitive() || property.PropertyType.IsEnumerable())
    //        return next(field, parameter, type, next);

    //    var subEntityParam = Expression.MakeMemberAccess(parameter, property);

    //    var pipeline = ComposeDefault();
    //    return MemberInitBuild(field.SelectionSet, subEntityParam, property.PropertyType, pipeline);
    //};

    //public readonly static PipelineDelegate CollectionProcessor = (field, parameter, type, context) =>
    //{
    //    var fieldName = field.Name.StringValue;
    //    var property = type.GetProperty(fieldName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance) ??
    //        throw new NullReferenceException();

    //    var propType = property.PropertyType;
    //    if (propType.IsEnumerable())
    //    {
    //        var subEntityType = propType.GenericTypeArguments.FirstOrDefault();
    //        var childParameter = Expression.Parameter(subEntityType);

    //        var pipeline = ComposeDefault();
    //        var childMemberInit = MemberInitBuild(field.SelectionSet, childParameter, subEntityType, pipeline);

    //        var selectMethod = typeof(Enumerable).GetMethods()
    //            .First(m => m.Name == "Select" && m.GetParameters().Length == 2)
    //            .MakeGenericMethod(subEntityType, childMemberInit.Type);

    //        var selectorLambda = Expression.Lambda(childMemberInit, childParameter);
    //        var selectCall = Expression.Call(selectMethod, Expression.PropertyOrField(parameter, property.Name), selectorLambda);

    //        return selectCall;
    //    }

    //    return context(field, parameter, type, context);
    //};

    //public readonly static PipelineDelegate TerminalProcessor = (field, parameter, type, context) =>
    //{
    //    return Expression.Empty();
    //};
}