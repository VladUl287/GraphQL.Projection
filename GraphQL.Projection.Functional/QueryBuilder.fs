module QueryBuilder

open System.Linq
open GraphQLProcessing
open ExpressionBuilderModule
open GraphQLOp

type QueryContext<'a> = {
    GraphQL: GraphQLOperations
    Query: QueryOperations<'a>
}

let project<'a> (ctx: QueryContext<'a>) (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let normalized = ctx.GraphQL.Normilize ast
    let node = ctx.GraphQL.Interpret normalized

    let selectExp = ctx.Query.Select node
    let whereExpr = ctx.Query.Where node
    let orderByExpr = ctx.Query.OrderBy node
    query
        .Where(whereExpr)
        .OrderBy(orderByExpr.Expression)
        .Select(selectExp)