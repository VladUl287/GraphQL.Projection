module Projection

open System.Linq
open GraphQLOp
open GraphQLSystem
open ExpressionBuilder
open GraphQLOp.Operations

type GraphQLPipeline = GraphQLOp<GraphQLNode> -> GraphQLNode
type QueryBuilderFactory<'a> = GraphQLNode -> Builder<'a>

type QueryProjectionContext<'a> = {
    graphQLPipeline: GraphQLPipeline 
    createQueryBuilder: QueryBuilderFactory<'a>
}

let project<'a> (ctx: QueryProjectionContext<'a>) (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let builder = ast |> ctx.graphQLPipeline |> ctx.createQueryBuilder
    builder.Invoke(query)

let projectTo<'a> (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let builderCtx: BuilderContext = {
        typeInspector = TypeSystem.defaultInspector
        typeFactory = AnonymousTypeBuilder.createAnonymousType
    }

    let pipeline: GraphQLPipeline = fun op -> 
        op
        |> map prune
        |> map (flatten typeof<'a> TypeSystem.defaultInspector)
        |> interpret

    let projectionCtx: QueryProjectionContext<'a> = {
        graphQLPipeline = pipeline
        createQueryBuilder = builderFactory<'a> builderCtx
    }

    project<'a> projectionCtx ast query