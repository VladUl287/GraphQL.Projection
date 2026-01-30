open System
open System.Reflection
open System.Linq.Expressions
open GraphQLOp
open GraphQLOp.Operations

let userQuery = 
    object' "user" [
        field "id" []
        field "name" []
        //object' "phone" [
        //    field "country" []
        //    field "number" []
        //]
    ]

let ast = interpret userQuery
printfn "AST: %A" ast

let buildSelector<'a, 'b> (node: GraphQLNode) : Expression<Func<'a, 'b>> =
    let parameter = Expression.Parameter(typeof<'a>)

    let rec toMemberBinding (currentType: Type) (param: Expression) (node: GraphQLNode): MemberBinding = 
        match node with
            | FieldNode(name, args) -> 
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                let access = Expression.Property(param, property) :> Expression
                Expression.Bind(property, access) :> MemberBinding
            | ObjectNode(name, selections) -> 
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                let nestedAccess = Expression.Property(param, property)
                let nestedType = property.PropertyType
                let bindings = selections |> List.map (toMemberBinding nestedType nestedAccess)
                let memberInit = Expression.MemberInit(Expression.New(nestedType), bindings) :> Expression
                Expression.Bind(property, memberInit) :> MemberBinding
                //Expression.MemberBind(property, bindings) :> MemberBinding

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

                let ctor = typeof<'b>.GetConstructors().[0]

                let members = 
                    selections |> List.map (fun selection ->
                        let expr = toExpression nestedType nestedAccess selection false
                        match expr with
                        | :? MemberExpression as memberExpr -> Expression.Bind(memberExpr.Member, memberExpr) :> MemberBinding
                        | _ -> failwithf "Expected property access, got %A" expr.NodeType
                    )
                Expression.MemberInit(Expression.New(nestedType), members) :> Expression

    let body = toExpression typeof<'a> parameter node true
    Expression.Lambda<Func<'a, 'b>>(body, parameter)

type User = { Id: int; Name: string; Email: string }

let selector = buildSelector<User, User> ast

//let toExpression<'a> (node: GraphQLNode): Expression = 
//    match node with
//        | FieldNode(name, args) -> 
//            let property = typeof<'a>.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
//            let parameter = Expression.Parameter(typeof<'a>)
//            Expression.Property(parameter, property) :> Expression
//        | ObjectNode(name, selections) -> Expression.Empty() :> Expression

//let expression = map (toExpression) userQuery