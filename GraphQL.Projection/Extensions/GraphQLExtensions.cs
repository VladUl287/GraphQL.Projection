using GraphQLParser.AST;
using GraphQL.Projection.Helpers;

namespace GraphQL.Projection.Strategy.Extensions;

public static class GraphQLExtensions
{
    public static IEnumerable<EntityField> ToFields<TEntity>(this string query)
    {
        var document = GraphQLParser.Parser.Parse(query);

        return ToFields<TEntity>(document);
    }

    public static IEnumerable<EntityField> ToFields<TEntity>(this GraphQLDocument document)
    {
        return GraphQLHelper.GetFields<TEntity>(document);
    }
}
