using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public interface IBindingStrategy
{
    bool AppliesTo(Type type);

    MemberBinding Bind(PropertyInfo property, Expression parameter, EntityField field, Func<Type, Expression, IEnumerable<EntityField>, MemberInitExpression> memberInit);
}
