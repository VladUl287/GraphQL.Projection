module ExpressionBuilder

open GraphQLProcessing
open System.Linq.Expressions
open System

type ExpressionBuilderContext = {
    TypeInspector: TypeSystem.TypeInspector
    NodeProcessor: GraphQLProcessing.NodeProcessor
    GraphQLOperations: GraphQLOp.GraphQLOpOperations
    CreateAnonymousType: AnonymousTypeBuilder.Builder
}

let buildSelector<'a> (context: ExpressionBuilderContext) (node: GraphQLNode) : Expression<Func<'a, obj>> =
    let parameter = Expression.Parameter(typeof<'a>)

    Expression.Lambda<Func<'a, obj>>(parameter, [])