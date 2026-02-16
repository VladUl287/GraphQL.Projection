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
    let { GraphQL = graph; Query = queryOp } = ctx

    let normalized = graph.Normilize ast
    let node = graph.Interpret normalized

    let builder = queryOp.Build node
    builder.Compile().Invoke(query)