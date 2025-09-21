namespace GraphQL.Projection.Resolvers.Contracts;

public interface ITypeResolver
{
    Type? GetPropertyType(Type type);
}
