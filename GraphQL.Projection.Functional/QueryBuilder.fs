module QueryProjection

open GraphQLOp
open System.Linq
open GraphQLProcessing
open ExpressionSystem

type QueryContext<'a> = {
    GraphQL: GraphQLOperations
    QueryFactory: BuilderFactory<'a>
}

let project<'a> (ctx: QueryContext<'a>) (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let { GraphQL = graph; QueryFactory = factory } = ctx

    let normalized = graph.Normilize ast
    let node = graph.Interpret normalized

    let builder = factory.Create node

    builder.Invoke(query)