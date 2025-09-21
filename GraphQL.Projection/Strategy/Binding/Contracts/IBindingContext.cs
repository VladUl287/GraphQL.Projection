using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy.Binding.Contracts;

public interface IBindingContext
{
    MemberAssignment Bind(Expression parameter, PropertyInfo property, MemberInitExpression memberInit);
}
