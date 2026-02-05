module GraphQLProcessing

open System
open System.Reflection
open System.Collections
open TypeSystem

type GraphQLNode =
    | FieldNode of 
         name: string * 
         arguments: ArgumentNode list *
         alias: string option * 
         directives: DirectiveNode list * 
         selections: GraphQLNode list
    
    | InlineFragmentNode of 
         typeCondition: string option *
         directives: DirectiveNode list * 
         selections: GraphQLNode list

    //| FragmentSpread of 
    //   name: string * 
    //   directives: DirectiveNode list

    //| FragmentDefinition of 
    //    name: string * 
    //    typeCondition: string * 
    //    directives: DirectiveNode list * 
    //    selectionSet: GraphQLNode list

    //| OperationDefinitionNode of
    //    operationType: OperationType *
    //    name: string option *
    //    variableDefinitions: VariableDefinitionNode list * 
    //    directives: DirectiveNode list * 
    //    selections: GraphQLNode list

//and OperationType =
//    | Query
//    | Mutation
//    | Subscription

//and VariableDefinitionNode = {
//    Name: string
//    Type: ValueNode
//    DefaultValue: ArgumentNode option
//}

and ArgumentNode = {
    name: string
    value: ValueNode
}

and DirectiveNode = { 
    name: string; 
    arguments: ArgumentNode list 
}

and ValueNode =
    | Variable of name: string
    | IntValue of value: int
    | StringValue of value: string
    | BooleanValue of value: bool
    | NullValue
    | EnumValue of value: string
    | ListValue of values: ValueNode list
    | ObjectValue of fields: (string * ValueNode) list

type NodeProcessor = {
    FlattenFragments: GraphQLNode list -> Type -> GraphQLNode list
    GetPropertyTypes: GraphQLNode list -> Type -> (string * Type) list
}

let getPropertiesTypes (inspector: TypeInspector) (selections: GraphQLNode list) (targetType: Type): (string * Type) list =
    selections 
        |> List.choose (fun node -> 
            match node with
            | FieldNode (name, _, alias, _, _) -> 
                let property = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                if property <> null then
                    let propertyType = property.PropertyType
                    let finalType = 
                        match propertyType with
                            | t when inspector.IsPrimitive t -> t
                            | t when inspector.IsCollection t -> typeof<IEnumerable>
                            | _ -> typeof<obj>
                    let fieldName = if alias.IsSome then alias.Value else property.Name
                    Some (fieldName, finalType)
                else
                    None
            | _ -> None
        )

let rec flattenNodes (targetType: Type) (inspector: TypeInspector) (node: GraphQLNode): GraphQLNode = 
    match node with
        | FieldNode(_, _, _, _, fieldSelections) as primitiveField when List.isEmpty fieldSelections -> primitiveField
        | FieldNode(name, args, alias, directives, fieldSelections) -> 
            let prop = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            let propType = prop.PropertyType
            let flattenedSelections = 
                fieldSelections 
                    |> List.map (fun selection ->
                        flattenNodes propType inspector selection
                    )
            FieldNode(name, args, alias, directives, flattenedSelections)
        | InlineFragmentNode (typeCondition, _, fragmentSelections) as fragNode ->
            if inspector.IsSubtypeOf targetType typeCondition.Value 
            then 
                let flattenedSelections = 
                    fragmentSelections 
                        |> List.map (fun selection ->
                            flattenNodes targetType inspector selection
                        )
                InlineFragmentNode(typeCondition, )
            else
                fragNode

let rec flattenNodes (targetType: Type) (inspector: TypeInspector) (node: GraphQLNode): GraphQLNode list = 
    match node with
        | FieldNode(_, _, _, _, fieldSelections) as primitiveField when List.isEmpty fieldSelections -> [primitiveField]
        | FieldNode(name, args, alias, directives, fieldSelections) -> 
            let prop = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            let propType = prop.PropertyType
            
            let flattenedSelections = 
                fieldSelections 
                    |> List.collect (flattenNodes propType inspector)
            
            [FieldNode(name, args, alias, directives, flattenedSelections)]
        | InlineFragmentNode(typeCondition, directives, fragmentSelections) ->
            if inspector.IsSubtypeOf targetType typeCondition.Value then
                fragmentSelections 
                    |> List.collect (flattenNodes targetType inspector)
            else
                []
 
let flattenNode (targetType: Type) (inspector: TypeInspector) (node: GraphQLNode): GraphQLNode = 
    let flattened = flattenNodes targetType inspector node
    match flattened with
        | [single] -> single
        | [] -> FieldNode("__empty", [], None, [], [])
        | multiple -> FieldNode("__container", [], None, [], multiple)

let rec flattenFragments (selections: GraphQLNode list) (targetType: Type) (inspector: TypeInspector): GraphQLNode list = 
    selections 
        |> List.collect (fun selection ->
            match selection with
                | FieldNode(_, _, _, _, fieldSelections) when List.isEmpty fieldSelections -> [selection]
                | FieldNode(name, _, _, _, fieldSelections) -> 
                    let prop = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                    let propType = prop.PropertyType
                    flattenFragments fieldSelections propType inspector
                | InlineFragmentNode (typeCondition, _, fragmentSelections) ->
                    if inspector.IsSubtypeOf targetType typeCondition.Value 
                    then flattenFragments fragmentSelections targetType inspector
                    else []
            )