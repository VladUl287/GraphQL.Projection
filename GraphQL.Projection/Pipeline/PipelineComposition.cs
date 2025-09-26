using GraphQL.Projection.Models;
using GraphQLParser.AST;

namespace GraphQL.Projection.Pipeline;

public static class PipelineComposition
{
    public static GraphQLFeatureModule<TEntity> Compose<TEntity>(params GraphQLFeatureModule<TEntity>[] modules)
    {
        return modules.Aggregate((first, second) => (doc, model) => second(doc, first(doc, model)));
    }

    public static GraphQLFeatureModule<TEntity> CreatePipeline<TEntity>()
    {
        var select = SelectFeatureModule.Create<TEntity>();
        return Compose(select);
    }

    public static QueryModel<TEntity> ExecPipeline<TEntity>(GraphQLDocument doc, QueryModel<TEntity> model, GraphQLFeatureModule<TEntity>[] modules, int index = 0)
    {
        if (index >= modules.Length) return model;
        model = modules[index].Invoke(doc, model);
        return ExecPipeline(doc, model, modules, index + 1);
    }
}
