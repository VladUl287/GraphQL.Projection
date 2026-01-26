using GraphQL.Projection.Resolvers.Contracts;
using GraphQL.Projection.Strategy.Binding.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Strategy.Binding;

public sealed class EnumerableStrategy(IParameterResolver parameterResolver) : IBindingStrategy
{
    private readonly IParameterResolver parameterResolver = parameterResolver;

    public bool AppliesTo(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    public MemberAssignment Bind(Expression initialParameter, MemberInfo property, MemberInitExpression memberInit)
    {
        var elementType = property.DeclaringType?
            .GetGenericArguments()
            .FirstOrDefault() ?? throw new InvalidOperationException("Generic type for collection not founded.");

        var selectMethod = typeof(Enumerable).GetMethods()
            .Where(m => m.Name == nameof(Enumerable.Select))
            .First(m => m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType, elementType);

        var parameter = parameterResolver.GetParameterExpression(memberInit);

        var selectLambda = Expression.Lambda(memberInit, parameter);

        var memberAccess = initialParameter;

        var call = Expression.Call(selectMethod, memberAccess, selectLambda);

        return Expression.Bind(property, call);
    }
}
