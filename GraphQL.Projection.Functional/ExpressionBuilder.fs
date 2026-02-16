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

type SortDirection = Ascending | Descending

type OrderByOperation<'a> = {
    Expression: Expression<Func<'a, obj>>
    Direction: SortDirection
}

//type PaginationOperation<'a> = {
//    Take: int
//    Skip: int option
//    After: obj option
//}

type QueryOperations<'a> = {
    Select: GraphQLNode -> Expression<Func<'a, obj>>
    Where: GraphQLNode -> Expression<Func<'a, bool>>
    OrderBy: GraphQLNode -> OrderByOperation<'a>

    //Pagination : PaginationOperation option
    //Distinct : bool
    //GroupBy : Expression<Func<'a, obj>> option
}