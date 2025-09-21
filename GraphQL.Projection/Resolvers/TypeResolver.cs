using GraphQL.Projection.Resolvers.Contracts;

namespace GraphQL.Projection.Resolvers;

public sealed class TypeResolver : ITypeResolver
{
    public Type GetPropertyType(Type propertyCoreType)
    {
        ArgumentNullException.ThrowIfNull(propertyCoreType, nameof(propertyCoreType));

        if (propertyCoreType.IsGenericType && propertyCoreType.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            return propertyCoreType.GetGenericArguments()
                .FirstOrDefault() ?? throw new InvalidOperationException("Generic type for collection not founded.");
        }

        if (propertyCoreType.IsGenericType && propertyCoreType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return propertyCoreType.GetGenericArguments()
                .FirstOrDefault() ?? throw new InvalidOperationException("Generic type for collection not founded.");
        }

        return propertyCoreType;
    }
}
