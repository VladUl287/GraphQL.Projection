module QueryProjection

open GraphQLOp
open System.Linq
open GraphQLProcessing
open ExpressionSystem

type QueryContext<'a> = {
    GraphQL: GraphQLOperations
}

let project<'a> (ctx: QueryContext<'a>) (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let { GraphQL = graph; } = ctx

    let normalized = graph.Normilize ast
    let node = graph.Interpret normalized

    let builder = createFactory<'a> node

    builder.Invoke(query)