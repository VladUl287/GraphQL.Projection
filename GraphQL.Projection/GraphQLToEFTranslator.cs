using GraphQL.Projection.Models;
using GraphQLParser.AST;

namespace GraphQL.Projection;

public static class GraphQLToEFTranslator
{
    public static IQueryable<TEntity> Translate<TEntity>(this IQueryable<TEntity> query, GraphQLDocument doc, GraphQLFeatureModule pipeline)
    {
        //var queryModel = pipeline(doc, QueryModel<TEntity>.Empty);

        //query = query.Select(queryModel.Select);

        return query;
    }
}
