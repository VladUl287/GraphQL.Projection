module Projection

open System.Linq
open GraphQLOp
open GraphQLSystem
open ExpressionSystem

type QueryContext<'a> = {
    graphOperations: GraphQLOperations
    builderFactory: BuilderFactory<'a>
    Expression: ExpressionContext
}

let project<'a> (ctx: QueryContext<'a>) (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let { graphOperations = graph; builderFactory = factory; Expression = exprCtx } = ctx

    let normalized = graph.normilize ast
    let node = graph.interpret normalized

    let builder = factory exprCtx node 

    builder.Invoke(query)

let projectTo<'a> (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    
    null