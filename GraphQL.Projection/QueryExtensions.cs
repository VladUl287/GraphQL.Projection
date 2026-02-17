using static GraphQLSystem;
using Microsoft.FSharp.Core;
using static GraphQLProcessing;
using static QueryProjection;

namespace GraphQL.Projection;

public static class QueryExtensions
{
    public static IQueryable<object> ProjectTo<T>(this IQueryable<T> query, GraphQLOp<GraphQLNode> op)
    {
        Func<GraphQLOp<GraphQLNode>, GraphQLOp<GraphQLNode>> normilize = (a) =>
        {
            Func<GraphQLNode, GraphQLNode> prune = Operations.prune;
            var updatedQuery = Operations.map(FuncConvert.FromFunc(prune), a);

            Func<GraphQLNode, GraphQLNode> flatten = (a) => Operations.flatten(typeof(T), TypeSystem.defaultInspector, a);
            updatedQuery = Operations.map(FuncConvert.FromFunc(flatten), updatedQuery);

            return updatedQuery;
        };

        Func<GraphQLOp<GraphQLNode>, GraphQLNode> interpret = Operations.interpret;
        var graphQLOperations = new GraphQLOperations(FuncConvert.FromFunc(normilize), FuncConvert.FromFunc(interpret));

        var expressionContext = new ExpressionSystem.ExpressionContext(TypeSystem.defaultInspector, null, null);

        var queryContext = new QueryContext<T>(graphQLOperations, ExpressionSystem.defaultFactory<T>(), expressionContext);

        return project<T>(queryContext, op, query);
    }
}
