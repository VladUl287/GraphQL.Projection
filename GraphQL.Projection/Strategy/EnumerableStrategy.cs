using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public sealed class EnumerableStrategy : IBindingStrategy
{
    public bool AppliesTo(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

    public MemberBinding Bind(PropertyInfo property, Expression parameter, EntityField field, Func<Type, Expression, IEnumerable<EntityField>, MemberInitExpression> memberInit)
    {
        var elementType = property.PropertyType
            .GetGenericArguments()
            .FirstOrDefault() ?? throw new InvalidOperationException($"Generic type for collection {property.Name} not founded.");

        var selectMethod = typeof(Enumerable).GetMethods()
            .Where(m => m.Name == nameof(Enumerable.Select))
            .First(m => m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType, elementType);

        var lambdaParameter = Expression.Parameter(elementType);

        var lambdaBody = memberInit(elementType, lambdaParameter, field.SubFields);

        var selectLambda = Expression.Lambda(lambdaBody, lambdaParameter);

        var memberAccess = Expression.Property(parameter, property);

        var call = Expression.Call(selectMethod, memberAccess, selectLambda);

        return Expression.Bind(property, call);
    }
}
