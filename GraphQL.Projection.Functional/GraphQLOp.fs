module GraphQLOp

open GraphQLProcessing
open System
open TypeSystem
open System.Reflection

type GraphQLOp<'a> =
    | Field of 
        name: string * 
        arguments: ArgumentNode list * 
        alias: string option * 
        directives: DirectiveNode list * 
        selections: GraphQLOp<'a> list * 
        next: (GraphQLNode -> 'a)
    | InlineFragment of 
        typeCondition: string option * 
        directives: DirectiveNode list * 
        selections: GraphQLOp<'a> list * 
        next: (GraphQLNode -> 'a)

module Operations =
    let rec map (f: 'a -> 'b) (op: GraphQLOp<'a>) : GraphQLOp<'b> =
        match op with
        | Field(name, args, alias, directives, selections, next) -> Field(name, args, alias, directives, List.map (map f) selections, next >> f)
        | InlineFragment(typeCondition, directives, selections, next) -> InlineFragment(typeCondition, directives, List.map (map f) selections, next >> f)
    
    let field name args alias directives selections = 
        Field(name, args, alias, directives, selections, fun node -> node)
    
    let inlineFragment typeCondition directives selections = 
        InlineFragment(typeCondition, directives, selections, fun node -> node)
    
    let rec interpret (op: GraphQLOp<GraphQLNode>) : GraphQLNode =
        match op with
        | Field(name, args, alias, directives, selections, next) -> 
            let nodes = selections |> List.map interpret
            next (FieldNode(name, args, alias, directives, nodes))
        | InlineFragment(typeCondition, directives, selections, next) -> 
            let nodes = selections |> List.map interpret
            next (InlineFragmentNode(typeCondition, directives, nodes))

    let rec flatten (targetType: Type) (inspector: TypeInspector) (op: GraphQLOp<'a>): GraphQLOp<'a> =       
        match op with
        | Field(_, _, _, _, selections, _)  as primitiveField when List.isEmpty selections -> 
            primitiveField
        | Field(name, args, alias, directives, selections, next) ->
            let prop = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            let propType = if prop <> null then prop.PropertyType else targetType
            
            let flattenedSelections = 
                selections 
                |> List.map (flatten propType inspector)
                |> List.collect (function
                    | InlineFragment(typeCondition, fragDirectives, fragSelections, fragNext) ->
                        match typeCondition with
                            | Some conditionType ->
                                if inspector.IsSubtypeOf propType conditionType then
                                    fragSelections |> List.map (flatten propType inspector)
                                else
                                    []
                            | None -> 
                                fragSelections |> List.map (flatten propType inspector)
                    | other -> [other]
                )
            
            Field(name, args, alias, directives, flattenedSelections, next)
        | InlineFragment(typeCondition, directives, selections, next) ->
            match typeCondition with
                | Some conditionType ->
                    if inspector.IsSubtypeOf targetType conditionType then
                        let flattenedSelections = 
                            selections 
                            |> List.collect (function
                                | InlineFragment(typeCondition, fragDirectives, fragSelections, fragNext) ->
                                    match typeCondition with
                                        | Some conditionType ->
                                            if inspector.IsSubtypeOf targetType conditionType then
                                                fragSelections |> List.map (flatten targetType inspector)
                                            else
                                                []
                                        | None -> 
                                            fragSelections |> List.map (flatten targetType inspector)
                                | other -> [other]
                            )
                        InlineFragment(typeCondition, directives, flattenedSelections, next)
                    else
                        InlineFragment(typeCondition, directives, [], next)
                | None ->
                    let flattenedSelections = 
                        selections 
                        |> List.collect (function
                            | InlineFragment(typeCondition, fragDirectives, fragSelections, fragNext) ->
                                match typeCondition with
                                    | Some conditionType ->
                                        if inspector.IsSubtypeOf targetType conditionType then
                                            fragSelections |> List.map (flatten targetType inspector)
                                        else
                                            []
                                    | None -> 
                                        fragSelections |> List.map (flatten targetType inspector)
                            | other -> [other]
                        )
                    InlineFragment(typeCondition, directives, flattenedSelections, next)

    