open System
open System.Reflection
open System.Linq.Expressions
open GraphQLOp
open GraphQLOp.Operations
open CretaeAnonymousType

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

let isBuiltInType (typ: Type) =
    typ.Namespace = "System" && not typ.IsClass
    || typ.IsPrimitive 
    || typ = typeof<string>
    || typ = typeof<DateTime>
    || typ = typeof<DateTimeOffset>
    || typ = typeof<TimeSpan>
    || typ = typeof<Guid>
    || typ = typeof<decimal>
    || typ = typeof<obj>

let getPropertyTypes (propsNames: string list) (targetType: Type) =
    propsNames |> List.choose(fun propName -> 
        let propInfo = targetType.GetProperty(propName, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
        if propInfo <> null then
            let propType = propInfo.PropertyType
            let finalType = if isBuiltInType propType then propType else typeof<obj>
            Some (propName, finalType)
        else 
            None
    )

let buildSelector<'a> (node: GraphQLNode) : Expression<Func<'a, obj>> =
    let parameter = Expression.Parameter(typeof<'a>)

    let rec toExpression (currentType: Type) (param: Expression) (node: GraphQLNode) (root: bool): Expression = 
        match node with
            | FieldNode(name, args) -> 
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                Expression.Property(param, property) :> Expression
            | ObjectNode(name, selections) -> 
                let access = 
                    match root with
                        | true -> param
                        | false -> 
                            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                            Expression.Property(param, property)

                let accessType = access.Type

                let propertiesNames = selections |> List.map (fun selection -> selection.Name)

                let properties = getPropertyTypes propertiesNames accessType

                let anonType = createAnonymousType properties

                let ctor = anonType.GetConstructors().[0]

                let members = 
                    selections |> List.map (fun selection ->
                        toExpression accessType access selection false
                    )

                Expression.New(ctor, members)

    let body = toExpression typeof<'a> parameter node true
    Expression.Lambda<Func<'a, obj>>(body, parameter)

type Phone = { Country: string; Number: string; }
type User = { Id: int; Name: string; Phone: Phone }

let selector = buildSelector<User> ast
printfn "\nSelector: %A" selector
