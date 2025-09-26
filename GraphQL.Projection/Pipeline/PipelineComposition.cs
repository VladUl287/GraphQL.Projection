using GraphQL.Projection.Models;

namespace GraphQL.Projection.Pipeline;

public static class PipelineComposition
{
    public static GraphQLFeatureModule<TEntity> Compose<TEntity>(params GraphQLFeatureModule<TEntity>[] modules)
    {
        return modules.Aggregate((first, second) =>
        {
            return (doc, model) =>
            {
                return second(doc, first(doc, model));
            };
        });
    }

    public static GraphQLFeatureModule<TEntity> CreatePipeline<TEntity>()
    {
        var select = SelectFeatureModule.Create<TEntity>();

        return Compose(select);
    }
}
