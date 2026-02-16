module QueryBuilder

open GraphQLOp
open System.Linq
open GraphQLProcessing
open ExpressionBuilderModule

type QueryBuilderContext<'a> = {
    GraphQL: GraphQLOpOperations
    Expressions: ExpressionBuilderOp<'a>
}

let project<'a> (ctx: QueryBuilderContext<'a>) (op: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> = 
    let processedOp = ctx.GraphQL.Normalize op

    let ast = ctx.GraphQL.Interpret processedOp

    let selectExp = ctx.Expressions.BuildSelect ast
    let whereExp = ctx.Expressions.BuildWhere ast
    let orderBy = ctx.Expressions.BuildOrderBy ast

    query
        .Select(selectExp)
