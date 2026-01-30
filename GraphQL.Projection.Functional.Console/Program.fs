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

    let rec toExpression (currentType: Type) (param: Expression) (node: GraphQLNode) (root: bool): Expression = 
        match node with
            | FieldNode(name, args) -> 
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                Expression.Property(param, property) :> Expression
            | ObjectNode(name, selections) -> 
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)

                let nestedAccess = 
                    match root with
                       | true -> param
                       | false -> Expression.Property(param, property)

                let nestedType = nestedAccess.Type

                let ctor = nestedType.GetConstructors().[0]

                let members = 
                    selections |> List.map (fun selection ->
                        toExpression nestedType nestedAccess selection false
                    )

                Expression.New(ctor, members)

    let body = toExpression typeof<'a> parameter node true
    Expression.Lambda<Func<'a, 'b>>(body, parameter)

type Phone = { Country: string; Number: string; }
type User = { Id: int; Name: string; Phone: Phone }

let selector = buildSelector<User, User> ast
printfn "Selector: %A" selector

//let toExpression<'a> (node: GraphQLNode): Expression = 
//    match node with
//        | FieldNode(name, args) -> 
//            let property = typeof<'a>.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
//            let parameter = Expression.Parameter(typeof<'a>)
//            Expression.Property(parameter, property) :> Expression
//        | ObjectNode(name, selections) -> Expression.Empty() :> Expression

//let expression = map (toExpression) userQuery