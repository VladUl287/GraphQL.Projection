using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Strategy.Binding.Contracts;

public interface IBindingContext
{
    MemberAssignment Bind(Expression parameter, PropertyInfo property, MemberInitExpression memberInit);
}
