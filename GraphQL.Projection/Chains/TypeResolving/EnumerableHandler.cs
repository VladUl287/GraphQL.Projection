using System.Reflection;

namespace GraphQL.Projection.Chains.TypeResolving;

internal sealed class EnumerableHandler : AbstractHandler
{
    public override Type? Handle(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        var type = property.PropertyType;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()
                .FirstOrDefault() ?? throw new InvalidOperationException("Generic type for collection not founded.");
        }

        return base.Handle(property);
    }
}
