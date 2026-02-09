module ExpressionBuilder

open System
open GraphQLOp
open GraphQLProcessing
open System.Linq.Expressions

type ExpressionBuilderContext = {
    TypeInspector: TypeSystem.TypeInspector
    NodeProcessor: GraphQLProcessing.NodeProcessor
    GraphQLOperations: GraphQLOp.GraphQLOpOperations
    CreateAnonymousType: AnonymousTypeBuilder.Builder
}

let buildSelector<'a> (context: ExpressionBuilderContext) (node: GraphQLOp<GraphQLNode>) : Expression<Func<'a, obj>> =

    let processedNodes = 
        node 
        |> Operations.map context.GraphQLOperations.Prune
        |> Operations.map context.GraphQLOperations.Flatten

    let parameter = Expression.Parameter(typeof<'a>)

    let rec toExpression (targetType: Type) (param: Expression) (node: GraphQLNode): Expression = 
        Expression.Empty()

    Expression.Lambda<Func<'a, obj>>(parameter, [])