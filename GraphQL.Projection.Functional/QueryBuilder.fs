module QueryBuilder

open System.Linq
open GraphQLProcessing
open ExpressionBuilderModule
open GraphQLOp

type QueryContext<'a> = {
    GraphQL: GraphQLOperations
    Expressions: QueryExpressionBuilder<'a>
}

let project<'a> (ctx: QueryContext<'a>) (ast: GraphQLOp<GraphQLNode>) (query: IQueryable<'a>): IQueryable<obj> =
    let normalized = ctx.GraphQL.Normilize ast
    let node = ctx.GraphQL.Interpret normalized

    let selectExp = ctx.Expressions.BuildSelect node
    query.Select(selectExp)