using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

using MemberInitFunc = Func<Type, Expression, IEnumerable<TreeField>, MemberInitExpression>;

public interface IBindingContext
{
    MemberBinding Bind(PropertyInfo property, Expression parameter, TreeField field, MemberInitFunc memberInit);
}
