using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public interface IBindingStrategy
{
    bool AppliesTo(Type type);

    MemberBinding Bind(PropertyInfo property, Expression parameter, TreeField field, Func<Type, Expression, IEnumerable<TreeField>, MemberInitExpression> memberInit);
}
