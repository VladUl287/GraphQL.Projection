using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public sealed class EntityStrategy : IBindingStrategy
{
    public bool AppliesTo(Type type) => type.IsClass && type != typeof(string);

    public MemberBinding Bind(PropertyInfo property, Expression accessParam, Expression bindParam, Type type, IEnumerable<MemberBinding> bindings)
    {
        var memberInit = Expression.MemberInit(Expression.New(type), bindings);

        return Expression.Bind(property, memberInit);
    }
}
