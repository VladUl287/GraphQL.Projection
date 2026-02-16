module ExpressionBuilderModule

open System
open GraphQLProcessing
open System.Linq.Expressions

type ExpressionBuilderContext = {
    TypeInspector: TypeSystem.TypeInspector
    NodeProcessor: GraphQLProcessing.NodeProcessor
    AnonymousTypeFactory: AnonymousTypeBuilder.AnonymousTypeFactory
}

let buildSelect<'a> (node: GraphQLNode): Expression<Func<'a, obj>> = 
    let select = fun (arg: 'a) -> arg :> obj
    select

type QueryExpressionBuilder<'a> = {
    BuildSelect: GraphQLNode -> Expression<Func<'a, obj>>
    BuildWhere: GraphQLNode -> Expression<Func<'a, bool>>
    BuildOrderBy: GraphQLNode -> Expression<Func<'a, obj>>
}