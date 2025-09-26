using GraphQL.Projection.Models;
using GraphQLParser.AST;

namespace GraphQL.Projection.Pipeline;

public static class GraphQLQueryPipeline
{
    public static QueryModel<TEntity> BuildQueryModel<TEntity>(GraphQLDocument document, IEnumerable<GraphQLFeatureModule<TEntity>> modules)
    {
        var seed = QueryModel<TEntity>.Empty;
        foreach (var module in modules)
        {
            seed = module.Invoke(document, seed);
        }
        return seed;
        //return modules.Aggregate(QueryModel<TEntity>.Empty, (model, module) => module.Invoke(document, model));
    }
}
