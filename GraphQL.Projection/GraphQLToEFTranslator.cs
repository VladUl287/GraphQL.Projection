using GraphQL.Projection.Models;
using GraphQL.Projection.Pipeline;
using GraphQLParser.AST;

namespace GraphQL.Projection;

public static class GraphQLToEFTranslator
{
    public static IQueryable<TEntity> Translate<TEntity>(this IQueryable<TEntity> query, GraphQLDocument doc, GraphQLFeatureModule<TEntity> pipeline)
    {
        var queryModel = pipeline(doc, QueryModel<TEntity>.Empty);

        query = query.Select(queryModel.Select);

        return query;
    }

    public static IQueryable<TEntity> AssignQuery<TEntity>(this IQueryable<TEntity> query, GraphQLDocument doc)
    {
        var select = SelectFeatureModule.Create<TEntity>();

        var queryModel = PipelineComposition.ExecPipeline(doc, QueryModel<TEntity>.Empty, [select]);

        query = query.Select(queryModel.Select);

        return query;
    }
}
