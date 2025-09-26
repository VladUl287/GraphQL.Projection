using GraphQL.Projection.Models;

namespace GraphQL.Projection.Pipeline;

public static class SelectFeatureModule
{
    public static GraphQLFeatureModule<TEntity> Create<TEntity>()
    {
        return (document, model) =>
        {
            return model with
            {
                Select = (entity) => entity
            };
        };
    }
}