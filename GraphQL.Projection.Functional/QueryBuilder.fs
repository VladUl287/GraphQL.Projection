module QueryBuilder

open GraphQLOp
open System.Linq
open GraphQLProcessing
open ExpressionBuilderModule

type QueryBuilderContext<'a> = {
    GraphQL: GraphQLOpOperations
    Expressionss: ExpressionBuilderOp<'a>
}

let project<'a> (op: GraphQLOp<GraphQLNode>) (ctx: QueryBuilderContext<'a>) (query: IQueryable<'a>): IQueryable<obj> = 
    let interpreted = 
        op
        |> Operations.map ctx.GraphQL.Prune
        |> Operations.map ctx.GraphQL.Flatten
        |> ctx.GraphQL.Interpret

    let selectExp = ctx.Expressionss.BuildSelect interpreted
    let whereExp = ctx.Expressionss.BuildWhere interpreted
    let orderBy = ctx.Expressionss.BuildOrderBy interpreted

    query
        .Select(selectExp)
