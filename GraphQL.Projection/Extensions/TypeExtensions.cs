namespace GraphQL.Projection.Extensions;

internal static class TypeExtensions
{
    internal static bool IsPrimitive(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid);
    }
}
