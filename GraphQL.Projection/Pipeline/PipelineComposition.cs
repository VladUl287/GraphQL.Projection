using GraphQL.Projection.Models;

namespace GraphQL.Projection.Pipeline;

public static class PipelineComposition
{
    public static GraphQLFeatureModule Compose(params GraphQLFeatureModule[] modules)
    {
        return modules.Aggregate((first, second) => (doc, model) => second(doc, first(doc, model)));
    }

    public static GraphQLFeatureModule CreatePipeline(Type entity)
    {
        var defaultPipeline = SelectFeatureModule.CreateDefaultPipeline();
        var select = SelectFeatureModule.Create(entity, defaultPipeline);
        return Compose(select);
    }
}
