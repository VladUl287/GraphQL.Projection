module QueryProjection

open GraphQLSystem
open System.Linq
open GraphQLProcessing
open ExpressionSystem

type GraphQLOperations = {
    Normilize: GraphQLOp<GraphQLNode> -> GraphQLOp<GraphQLNode>
    Interpret: GraphQLOp<GraphQLNode> -> GraphQLNode
}

type QueryContext<'a> = {
    GraphQL: GraphQLOperations
    Factory: BuilderFactory<'a>
    Expression: ExpressionContext
}

let project<'a> (ctx: QueryContext<'a>) (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let { GraphQL = graph; Factory = factory; Expression = exprCtx } = ctx

    let normalized = graph.Normilize ast
    let node = graph.Interpret normalized

    let builder = factory.Create exprCtx node 

    builder.Invoke(query)