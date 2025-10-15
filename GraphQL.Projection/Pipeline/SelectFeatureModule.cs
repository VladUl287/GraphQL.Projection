using GraphQL.Projection.Extensions;
using GraphQL.Projection.Models;
using GraphQLParser.AST;
using LanguageExt;
using LanguageExt.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public delegate Expression PipelineStep(GraphQLField Field, Context Context);
    public delegate PipelineStep PipelineComposer(PipelineStep next);

    public sealed record Context(Expression Parameter, Type Type, PipelineStep Pipeline);

    public static PipelineStep Compose(PipelineStep terminal, PipelineComposer[] composers) =>
        composers.Aggregate(terminal, (current, nextComposer) => nextComposer(current));

    public static PipelineStep CreateDefaultPipeline() => Compose(TerminalStep, [CollectionComposer, EntityComposer, PrimitiveComposer]);

    public static GraphQLFeatureModule Create(Type type, PipelineStep pipeline)
    {
        var parameter = Expression.Parameter(type);
        var genericLambda = typeof(Func<,>).MakeGenericType(type, type);
        return (set, model) =>
        {
            var memberInit = CreateMemberInitialization(set, parameter, type, pipeline);
            var expression = Expression.Lambda(genericLambda, memberInit, parameter);
            return model with
            {
                Select = expression
            };
        };
    }

    private static MemberInitExpression CreateMemberInitialization(
        GraphQLSelectionSet selectionSet,
        Expression sourceParameter,
        Type targetType,
        PipelineStep pipeline)
    {
        var fieldBindings = CreateFieldBindings(selectionSet, sourceParameter, targetType, pipeline);
        var constructorCall = Expression.New(targetType);
        return Expression.MemberInit(constructorCall, fieldBindings);
    }

    private static IEnumerable<MemberBinding> CreateFieldBindings(
        GraphQLSelectionSet selectionSet,
        Expression sourceParameter,
        Type targetType,
        PipelineStep pipeline)
    {
        return selectionSet.Selections
            .OfType<GraphQLField>()
            .Select(field => CreateFieldBinding(field, sourceParameter, targetType, pipeline));
    }

    private static MemberAssignment CreateFieldBinding(
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

    private static Either<Error, MemberBinding> CreateFieldBindingFunctional(
        GraphQLField field,
        Expression parameter,
        Type type,
        PipelineStep pipeline)
    {
        return Prelude
            .Try<MemberBinding>(() =>
            {
                var property = type.GetProperty(field.Name.StringValue, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)
                    ?? throw new NullReferenceException();

                var context = new Context(parameter, type, pipeline);
                var propertyExpression = pipeline(field, context);
                return Expression.Bind(property, propertyExpression);
            })
            .ToEither()
            .MapLeft(ex => Error.New($"Failed to create binding for field {field.Name}: {ex.Message}"));
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
        var memberInit = CreateMemberInitialization(field.SelectionSet, childParameter, property.PropertyType, context.Pipeline);
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

            var memberInit = CreateMemberInitialization(field.SelectionSet, childParameter, subEntityType, context.Pipeline);

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