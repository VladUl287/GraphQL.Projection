using GraphQL.Projection.Helpers;

namespace GraphQL.Projection.Strategy.Extensions;

public static class GraphQLExtensions
{
    public static IEnumerable<TreeField> ToTree<TEntity>(this string query, IReadOnlyList<string> path)
    {
        var document = GraphQLParser.Parser.Parse(query);

        return GraphQLConverter.ConvertToTree<TEntity>(document, path);
    }
}
