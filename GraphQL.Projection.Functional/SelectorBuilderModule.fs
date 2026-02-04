module QueryBuilder

open System
open GraphQLOp
open System.Reflection
open System.Linq.Expressions
open CretaeAnonymousType
open System.Collections

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

let rec isSubtypeOf (targetType: Type) (typeName: string) =
    if targetType.Name = typeName then true
    elif targetType.BaseType <> null then 
        isSubtypeOf targetType.BaseType typeName
    else
        targetType.GetInterfaces()
            |> Array.exists (fun iface -> 
                if iface.Name = typeName then true
                elif isSubtypeOf iface typeName then true
                else false)

let rec flattenFragments (selections: GraphQLNode list) (targetType: Type): GraphQLNode list = 
    selections 
        |> List.collect (fun selection ->
            match selection with
                | FieldNode _ -> [selection]
                | InlineFragmentNode (typeCondition, _, selections) ->
                    if isSubtypeOf targetType typeCondition.Value 
                    then flattenFragments selections targetType
                    else []
            )
        |> List.filter (function
            | FieldNode _ -> true
            | InlineFragmentNode _ -> false)

let getPropertyTypes (selections: GraphQLNode list) (targetType: Type): (string * Type) list =
    selections 
        |> List.choose (fun node -> 
            match node with
            | FieldNode (name, _, alias, _, _) -> 
                let property = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                if property <> null then
                    let propertyType = property.PropertyType
                    let finalType = if isBuiltInType propertyType then propertyType else typeof<obj>
                    let fieldName = if alias.IsSome then alias.Value else property.Name
                    Some (fieldName, finalType)
                else
                    None
            | _ -> None
        )

let buildSelector<'a> (node: GraphQLNode) : Expression<Func<'a, obj>> =
    let parameter = Expression.Parameter(typeof<'a>)

    let rec toExpression (currentType: Type) (param: Expression) (node: GraphQLNode) (root: bool): Expression = 
        match node with
            | FieldNode(name, _, _, _, selections) when List.isEmpty selections ->
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                Expression.Property(param, property) :> Expression
            | FieldNode(name, args, alias, directives, selections) -> 
                if currentType.IsInstanceOfType(typeof<IEnumerable>) then 
                    Expression.Empty()
                else
                    let access = 
                        match root with
                            | true -> param
                            | false -> 
                                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                                Expression.Property(param, property)
                    
                    let accessType = access.Type
                    
                    let flatSelections = flattenFragments selections accessType
                            
                    let properties = getPropertyTypes flatSelections accessType
                    
                    //let anonType = createAnonymousType properties
                    let anonType = createJsonSerializableType properties
                    
                    let ctor = anonType.GetConstructors().[0]
                    
                    let members = 
                        selections |> List.map (fun selection ->
                            toExpression accessType access selection false
                        )
                    
                    //Expression.New(ctor, members)

                    let bindings = 
                        members
                        |> List.map (fun exp ->
                            Expression.Bind(anonType.GetProperties()[0], exp) :> MemberBinding
                        )

                    Expression.MemberInit(Expression.New(ctor), bindings) :> Expression
            | InlineFragmentNode(_, _, _) -> 
               Expression.Empty()

    let body = toExpression typeof<'a> parameter node true
    Expression.Lambda<Func<'a, obj>>(body, parameter)