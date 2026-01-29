open System
open System.Reflection
open System.Linq.Expressions
open GraphQLOp
open GraphQLOp.Operations

let userQuery = 
    object' "user" [
        field "id" []
        field "name" []
        object' "phone" [
            field "country" []
            field "number" []
        ]
    ]

let ast = interpret userQuery
printfn "AST: %A" ast

let buildSelector<'a, 'b> (node: GraphQLNode) : Expression<Func<'a, 'b>> =
    let parameter = Expression.Parameter(typeof<'a>)

    let toExpression (param: ParameterExpression) (node: GraphQLNode): Expression = 
        match node with
            | FieldNode(name, args) -> 
                let property = typeof<'a>.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                Expression.Property(param, property) :> Expression
            | ObjectNode(name, selections) -> Expression.Empty() :> Expression

    let body = toExpression parameter node
    Expression.Lambda<Func<'a, 'b>>(body, parameter)

let toExpression<'a> (node: GraphQLNode): Expression = 
    match node with
        | FieldNode(name, args) -> 
            let property = typeof<'a>.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            let parameter = Expression.Parameter(typeof<'a>)
            Expression.Property(parameter, property) :> Expression
        | ObjectNode(name, selections) -> Expression.Empty() :> Expression

let expression = map (toExpression) userQuery