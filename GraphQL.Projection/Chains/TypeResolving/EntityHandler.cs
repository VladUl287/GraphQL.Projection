using System.Reflection;

namespace GraphQL.Projection.Chains.TypeResolving;

internal sealed class EntityHandler : AbstractHandler
{
    public override Type? Handle(PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        var type = property.PropertyType;
        if (type.IsClass)
        {
            return type;
        }

        return base.Handle(property);
    }
}
