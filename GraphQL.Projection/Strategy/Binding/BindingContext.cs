using GraphQL.Projection.Strategy.Binding.Contracts;
using System.Linq.Expressions;
using System.Reflection;

namespace GraphQL.Projection.Strategy.Binding;

public sealed class BindingContext : IBindingContext
{
    private readonly IEnumerable<IBindingStrategy> strategies;

    public BindingContext(IEnumerable<IBindingStrategy> strategies)
    {
        this.strategies = strategies;
    }

    public MemberAssignment Bind(Expression parameter, PropertyInfo property, MemberInitExpression memberInit)
    {
        var strategy = strategies.FirstOrDefault(s => s.AppliesTo(property.PropertyType))
            ?? throw new NullReferenceException($"Binding startegy for {property.PropertyType} not registered.");

        return strategy.Bind(parameter, property, memberInit);
    }
}

