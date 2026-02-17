using static GraphQLOp;
using static Projection;
using static GraphQLSystem;

namespace GraphQL.Projection;

public static class QueryExtensions
{
    public static IQueryable<object> ProjectTo<T>(this IQueryable<T> query, GraphQLOp<GraphQLNode> op) => projectTo(op, query);
}
