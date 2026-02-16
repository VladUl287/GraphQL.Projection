module ExpressionBuilder

open System
open GraphQLOp
open GraphQLProcessing
open System.Linq.Expressions
open System.Reflection

type ExpressionBuilderContext = {
    TypeInspector: TypeSystem.TypeInspector
    NodeProcessor: GraphQLProcessing.NodeProcessor
    GraphQLOperations: GraphQLOp.GraphQLOpOperations
    AnonymousTypeFactory: AnonymousTypeBuilder.AnonymousTypeFactory
}

