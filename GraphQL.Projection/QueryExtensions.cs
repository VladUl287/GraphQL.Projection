using Microsoft.FSharp.Core;
using System.Linq.Expressions;
using static GraphQLOp;
using static GraphQLProcessing;

namespace GraphQL.Projection;

public static class QueryExtensions
{
    public static IQueryable<object> ProjectTo<T>(this IQueryable<T> query, GraphQLOp<GraphQLNode> op)
    {
        Func<GraphQLOp<GraphQLNode>, GraphQLOp<GraphQLNode>> normilize = (a) => a;
        Func<GraphQLOp<GraphQLNode>, GraphQLNode> interpret = Operations.interpret;
        var graphQLOperations = new GraphQLOperations(FuncConvert.FromFunc(normilize), FuncConvert.FromFunc(interpret));

        Func<GraphQLNode, Expression<Func<IQueryable<T>, IQueryable<object>>>> build = (node) => ExpressionBuilderModule.buildQuery<T>(node);
        var queryOperations = new ExpressionBuilderModule.QueryOperations<T>(FuncConvert.FromFunc(build));

        var queryContext = new QueryBuilder.QueryContext<T>(graphQLOperations, queryOperations);

        return QueryBuilder.project<T>(queryContext, op, query);
    }
}
