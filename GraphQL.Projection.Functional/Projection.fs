module Projection

open System.Linq
open GraphQLOp
open GraphQLSystem
open ExpressionBuilder
open GraphQLOp.Operations

type GraphInterpreter = GraphQLOp<GraphQLNode> -> GraphQLNode
type BuilderFactory<'a> = GraphQLNode -> Builder<'a>

type ProjectionContext<'a> = {
    interpret: GraphInterpreter 
    build: BuilderFactory<'a>
}

let project<'a> (ctx: ProjectionContext<'a>) (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let builder = ast |> ctx.interpret |> ctx.build
    builder.Invoke(query)
   
let defaultContext<'a>: ProjectionContext<'a> = {
    interpret = fun operation -> 
        operation
        |> map prune
        |> map (flatten typeof<'a> TypeSystem.defaultInspector)
        |> interpret
    build = builderFactory<'a> {
        typeInspector = TypeSystem.defaultInspector
        typeFactory = AnonymousTypeBuilder.createAnonymousType
    }
}

let projectTo<'a> (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> = project<'a> defaultContext ast query