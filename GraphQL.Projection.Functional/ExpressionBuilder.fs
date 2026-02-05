module ExpressionBuilder

open System

type ExpressionBuilderContext = {
    TypeInspector: TypeSystem.TypeInspector
    NodeProcessor: GraphQLProcessing.NodeProcessor
    CreateAnonymousType: AnonymousTypeBuilder.Builder
}