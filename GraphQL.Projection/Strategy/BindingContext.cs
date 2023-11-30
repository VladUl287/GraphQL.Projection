using System.Reflection;
using System.Linq.Expressions;

namespace GraphQL.Projection.Strategy;

public sealed class BindingContext : IBindingContext
{
    private readonly IEnumerable<IBindingStrategy> strategies;

    public BindingContext(IEnumerable<IBindingStrategy> strategies)
    {
        this.strategies = strategies;
    }

    public MemberBinding Bind(PropertyInfo property, Expression parameter, TreeField field, Func<Type, Expression, IEnumerable<TreeField>, MemberInitExpression> memberInit)
    {
        var strategy = strategies.FirstOrDefault(s => s.AppliesTo(property.PropertyType))
            ?? throw new NullReferenceException($"Binding startegy for {property.PropertyType} not registered.");

        return strategy.Bind(property, parameter, field, memberInit);
    }
}

