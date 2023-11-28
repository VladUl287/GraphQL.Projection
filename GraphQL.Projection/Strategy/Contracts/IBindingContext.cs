using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

using MemberInitFunc = Func<Type, Expression, IEnumerable<EntityField>, MemberInitExpression>;

public interface IBindingContext
{
    MemberBinding Bind(PropertyInfo property, Expression parameter, EntityField field, MemberInitFunc memberInit);
}
