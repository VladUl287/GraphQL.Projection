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