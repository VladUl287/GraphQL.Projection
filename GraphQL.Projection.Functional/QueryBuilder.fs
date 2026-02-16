module QueryBuilder

open GraphQLOp
open System.Linq
open GraphQLProcessing
open ExpressionBuilderModule

type QueryBuilderContext<'a> = {
    GraphQL: GraphQLOpOperations
    ExpressionBuilder: ExpressionBuilderOp<'a>
}

let project<'a> (query: IQueryable<'a>) (op: GraphQLOp<GraphQLNode>) (ctx: QueryBuilderContext<'a>): IQueryable<obj> = 
    let interpreted = 
        op
        |> Operations.map ctx.GraphQL.Prune
        |> Operations.map ctx.GraphQL.Flatten
        |> ctx.GraphQL.Interpret

    let selectExp = ctx.ExpressionBuilder.buildSelect interpreted
    let whereExp = ctx.ExpressionBuilder.buildWhere interpreted
    let orderBy = ctx.ExpressionBuilder.buildOrderBy interpreted

    query
        .Select(selectExp)
