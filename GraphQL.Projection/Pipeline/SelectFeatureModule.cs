using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQLParser.AST;
using LanguageExt.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public delegate Expression PipelineStep(GraphQLField field, Expression param, Type type, PipelineStep Next);
    
    public static PipelineStep Compose(params PipelineStep[] processors)
    {
        var pipeline = processors.Aggregate(TerminalStep, (currentStep, nextStep) =>
        {
            return (field, param, type, context) => nextStep(field, param, type, currentStep);
        });
        return pipeline;
    }

    public static PipelineStep CreateDefault() => Compose(
        CollectionStep, EntityStep, PrimitiveStep);

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
        var bindings = new List<MemberBinding>(selectionSet.Selections.Count);

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is GraphQLField field)
            {
                var property = type.GetProperty(field.Name.StringValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new NullReferenceException();

                var bind = ProcessField(field, property, parameter, type, pipeline);
                bindings.Add(bind);
            }
        }

        return Expression.MemberInit(Expression.New(type), bindings);
    }

    private static MemberAssignment ProcessField(
        GraphQLField field,
        PropertyInfo property,
        Expression parameter,
        Type type,
        PipelineStep pipeline)
    {
        var propertyExpression = pipeline(field, parameter, type, TerminalStep);
        return Expression.Bind(property, propertyExpression);
    }

    public readonly static PipelineStep PrimitiveStep = (field, parameter, type, next) =>
    {
        return next(field, parameter, type, next);
    };

    public readonly static PipelineStep EntityStep = (field, parameter, type, next) =>
    {
        return next(field, parameter, type, next);
    };

    public readonly static PipelineStep CollectionStep = (field, parameter, type, next) =>
    {
        return next(field, parameter, type, next);
    };

    public readonly static PipelineStep TerminalStep = (field, parameter, type, next) =>
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