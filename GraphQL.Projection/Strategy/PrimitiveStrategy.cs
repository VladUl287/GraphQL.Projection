using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public class PrimitiveStrategy : IBindingStrategy
{
    public bool AppliesTo(Type type) => type.IsPrimitive || type == typeof(string);

    public MemberBinding Bind(PropertyInfo property, Expression parameter, EntityField field, Func<Type, Expression, IEnumerable<EntityField>, MemberInitExpression> memberInit)
    {
        var memberExpression = Expression.Property(parameter, property.Name);

        return Expression.Bind(property, memberExpression);
    }
}
