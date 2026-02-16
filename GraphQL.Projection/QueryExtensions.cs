using static GraphQLOp;
using Microsoft.FSharp.Core;
using static GraphQLProcessing;

namespace GraphQL.Projection;

public static class QueryExtensions
{
    public static IQueryable<object> ProjectTo<T>(this IQueryable<T> query, GraphQLOp<GraphQLNode> op)
    {
        Func<GraphQLOp<GraphQLNode>, GraphQLOp<GraphQLNode>> normilize = (a) => a;
        Func<GraphQLOp<GraphQLNode>, GraphQLNode> interpret = Operations.interpret;
        var graphQLOperations = new GraphQLOperations(FuncConvert.FromFunc(normilize), FuncConvert.FromFunc(interpret));

        var queryContext = new QueryProjection.QueryContext<T>(graphQLOperations, ExpressionSystem.defaultFactory<T>());

        return QueryProjection.project<T>(queryContext, op, query);
    }
}
