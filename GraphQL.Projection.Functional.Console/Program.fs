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

    let rec toExpression (currentType: Type) (param: Expression) (node: GraphQLNode): Expression = 
        match node with
            | FieldNode(name, args) -> 
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                Expression.Property(param, property) :> Expression
            | ObjectNode(name, selections) -> 
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                let nestedAccess = Expression.Property(param, property)
                let nestedType = property.PropertyType
                let members = 
                    selections |> List.map (fun selection ->
                        toExpression nestedType nestedAccess selection

                        //let expr = toExpression nestedType nestedAccess selection
                        //// Assuming expr is a MemberExpression (property access)
                        //match expr with
                        //| :? MemberExpression as memberExpr ->
                        //    // Bind the member to its value expression
                        //    Expression.Bind(memberExpr.Member, memberExpr) :> MemberBinding
                        //| _ -> failwithf "Expected property access, got %A" expr.NodeType

                        //match selection with
                        //    | FieldNode(fieldName, _) ->
                        //        let fieldProperty = nestedType.GetProperty(selection, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                        //        let fieldAccess = Expression.Property(nestedAccess, fieldProperty)
                        //        Expression.Bind(fieldProperty, fieldAccess) :> MemberBinding
                        //    | ObjectNode(_, _) -> failwith "Deep nesting not implemented"
                    )
                //Expression.MemberInit(Expression.New(nestedType), members)
                Expression.Empty()

    let body = toExpression typeof<'a> parameter node
    Expression.Lambda<Func<'a, 'b>>(body, parameter)

//let toExpression<'a> (node: GraphQLNode): Expression = 
//    match node with
//        | FieldNode(name, args) -> 
//            let property = typeof<'a>.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
//            let parameter = Expression.Parameter(typeof<'a>)
//            Expression.Property(parameter, property) :> Expression
//        | ObjectNode(name, selections) -> Expression.Empty() :> Expression

//let expression = map (toExpression) userQuery