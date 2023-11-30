using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public class PrimitiveStrategy : IBindingStrategy
{
    public bool AppliesTo(Type type) => type.IsPrimitive || type == typeof(string);

    public MemberBinding Bind(PropertyInfo property, Expression parameter, TreeField field, Func<Type, Expression, IEnumerable<TreeField>, MemberInitExpression> memberInit)
    {
        var memberExpression = Expression.Property(parameter, property.Name);

        return Expression.Bind(property, memberExpression);
    }
}
