using System.Collections;
using System.Runtime.CompilerServices;

namespace GraphQL.Projection.Extensions;

internal static class TypeExtensions
{
    internal static bool IsPrimitive(this Type type) => type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid);
    internal static bool IsCollection(this Type type) => type.ImplementsGenericInterface(typeof(ICollection<>));
    internal static bool IsEnumerable(this Type type) => type.IsAssignableTo(typeof(IEnumerable));

    internal static bool ImplementsGenericInterface(this Type type, Type interfaceType)
    {
        if (type.IsGenericType(interfaceType))
            return true;

        foreach (var inter in type.GetInterfaces().AsSpan())
            if (inter.IsGenericType(interfaceType))
                return true;

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsGenericType(this Type type, Type genericType) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == genericType;
}
