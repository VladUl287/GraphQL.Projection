module QueryBuilder

open System
open GraphQLOp
open System.Reflection
open System.Linq.Expressions
open CretaeAnonymousType
open System.Collections
open System.Collections.Generic

let isPrimitive (typ: Type) = typ.IsPrimitive || typ = typeof<string>
let isCollection (typ: Type) = typ.IsAssignableTo(typeof<IEnumerable>)
let getElementType (typ: Type) =
    let a = typ.GetGenericTypeDefinition();
    match typ with
    | t when t.IsArray -> 
        Some (t.GetElementType())
    | t ->
        t.GetInterfaces()
        |> Array.tryPick (fun i ->
            if i.IsGenericType && 
               i.GetGenericTypeDefinition() = typedefof<IEnumerable<_>> then
               Some (i.GetGenericArguments()[0])
            else
               None)

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
                    let finalType = 
                        match propertyType with
                            | t when isPrimitive t -> t
                            | t when isCollection t -> typeof<IEnumerable>
                            | _ -> typeof<obj>
                    let fieldName = if alias.IsSome then alias.Value else property.Name
                    Some (fieldName, finalType)
                else
                    None
            | _ -> None
        )

//let getSelectMethod (sourceType: Type) (resultType: Type) =
//                       typeof<System.Linq.Enumerable>.GetMethods()
//                       |> Array.find (fun m ->
//                           m.Name = "Select" &&
//                           m.GetParameters().Length = 2 && 
//                           m.GetParameters()[0].ParameterType = typeof<System.Collections.Generic.IEnumerable<_>>.MakeGenericType([|sourceType|]) &&
//                           m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() = typeof<System.Func<_,_>>)
//                       |> fun m -> m.MakeGenericMethod([|sourceType; resultType|])

let buildSelector<'a> (node: GraphQLNode) : Expression<Func<'a, obj>> =
    let parameter = Expression.Parameter(typeof<'a>)

    let rec toExpression (currentType: Type) (param: Expression) (node: GraphQLNode) (root: bool): Expression = 
        match node with
            | FieldNode(name, _, _, _, selections) when List.isEmpty selections ->
                let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                Expression.Property(param, property) :> Expression
            | FieldNode(name, args, alias, directives, selections) -> 
                let access = 
                    match root with
                        | true -> param
                        | false -> 
                            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                            Expression.Property(param, property)
                
                let accessType = access.Type
                    
                if isCollection accessType then 
                    let collectionType = accessType
                    let elementType = (getElementType accessType).Value

                    let flatSelections = flattenFragments selections elementType
                            
                    let properties = getPropertyTypes flatSelections elementType
                    
                    let anonType = createAnonymousType properties
                    
                    let ctor = anonType.GetConstructors().[0]
                    
                    let subParameter = Expression.Parameter(elementType)

                    let members = 
                        flatSelections 
                        |> List.map (fun selection ->
                            toExpression elementType subParameter selection false
                        )
                                        
                    let bindings = 
                        members
                        |> List.mapi (fun index exp ->
                            Expression.Bind(anonType.GetProperties()[index], exp) :> MemberBinding
                        )

                    let memberInit = Expression.MemberInit(Expression.New(ctor), bindings)

                    let selectMethod = 
                        typeof<System.Linq.Enumerable>.GetMethods()
                        |> Array.find (fun m -> 
                            m.Name = "Select" && 
                            m.GetParameters().Length = 2)

                    let genericSelectMethod = selectMethod.MakeGenericMethod(elementType, memberInit.Type)

                    let lambda = Expression.Lambda(memberInit, subParameter)

                    Expression.Call(genericSelectMethod, access, lambda)
                else
                    let flatSelections = flattenFragments selections accessType
                            
                    let properties = getPropertyTypes flatSelections accessType
                    
                    let anonType = createAnonymousType properties
                    
                    let ctor = anonType.GetConstructors().[0]
                    
                    let members = 
                        flatSelections 
                        |> List.map (fun selection ->
                            toExpression accessType access selection false
                        )
                                        
                    let bindings = 
                        members
                        |> List.mapi (fun index exp ->
                            Expression.Bind(anonType.GetProperties()[index], exp) :> MemberBinding
                        )

                    Expression.MemberInit(Expression.New(ctor), bindings) :> Expression
            | InlineFragmentNode(_, _, _) -> 
               Expression.Empty()

    let body = toExpression typeof<'a> parameter node true
    Expression.Lambda<Func<'a, obj>>(body, parameter)