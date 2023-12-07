using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public interface IBindingContext
{
    MemberBinding Bind(PropertyInfo property, Expression accessParameter, Expression bindParameter, Type type, IEnumerable<MemberBinding> bindings);
}
