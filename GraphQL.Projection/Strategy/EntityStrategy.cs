using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public sealed class EntityStrategy : IBindingStrategy
{
    public bool AppliesTo(Type type) => type.IsClass && type != typeof(string);

    public MemberBinding Bind(PropertyInfo property, Expression parameter, TreeField field, Func<Type, Expression, IEnumerable<TreeField>, MemberInitExpression> memberInit)
    {
        var memberExpression = Expression.Property(parameter, property.Name);

        var subFieldsInit = memberInit(property.PropertyType, memberExpression, field.Children);

        return Expression.Bind(property, subFieldsInit);
    }
}
