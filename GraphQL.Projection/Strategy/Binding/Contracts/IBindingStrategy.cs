using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Strategy.Binding.Contracts;

public interface IBindingStrategy
{
    bool AppliesTo(Type type);

    MemberAssignment Bind(Expression parameter, MemberInfo member, MemberInitExpression memberInit);
}
