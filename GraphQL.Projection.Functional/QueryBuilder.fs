module QueryBuilder

open GraphQLOp
open System.Linq
open GraphQLProcessing
open ExpressionBuilderModule

type QueryBuilderContext<'a> = {
    GraphQL: GraphQLOpOperations
    Expressions: ExpressionBuilderOp<'a>
}

let project<'a> (op: GraphQLOp<GraphQLNode>) (ctx: QueryBuilderContext<'a>) (query: IQueryable<'a>): IQueryable<obj> = 
    let interpreted = 
        op
        |> Operations.map ctx.GraphQL.Prune
        |> Operations.map ctx.GraphQL.Flatten
        |> ctx.GraphQL.Interpret

    let selectExp = ctx.Expressions.BuildSelect interpreted
    let whereExp = ctx.Expressions.BuildWhere interpreted
    let orderBy = ctx.Expressions.BuildOrderBy interpreted

    query
        .Select(selectExp)
