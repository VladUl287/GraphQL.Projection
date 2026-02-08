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

    let rec prune (op: GraphQLNode): GraphQLNode =
        match op with 
        | FieldNode(name, args, alias, directives, selections) ->
            let prunedSelections = 
                selections
                |> List.filter (function
                    | FieldNode(_, _, _, directives, _)
                    | InlineFragmentNode(_, directives, _) ->
                        let structure = 
                            directives 
                            |> List.filter (fun d -> ["@include"; "@skip"] |> List.contains d.name)

                        structure.Length = 0 ||
                        structure |> List.forall(function
                            | directive when directive.name = "@include" -> 
                                directive.arguments 
                                |> List.tryItem 0
                                |> Option.exists (fun arg ->
                                    match arg.value with
                                    | BooleanValue includ -> not includ
                                    | _ -> true)
                            | directive when directive.name = "@skip" ->
                                directive.arguments 
                                |> List.tryItem 0
                                |> Option.exists (fun arg ->
                                    match arg.value with
                                    | BooleanValue skip -> skip
                                    | _ -> false)
                            | _ -> false
                        )
                )
            FieldNode(name, args, alias, directives, prunedSelections)
        | InlineFragmentNode(typeCondition, directives, selections) ->
            let prunedSelections = 
                selections
                |> List.filter (function
                    | FieldNode(_, _, _, directives, _)
                    | InlineFragmentNode(_, directives, _) ->
                        let structure = 
                            directives 
                            |> List.filter (fun d -> ["@include"; "@skip"] |> List.contains d.name)

                        structure.Length <> 0 &&
                        structure |> List.forall(function
                            | directive when directive.name = "@include" -> 
                                directive.arguments 
                                |> List.tryItem 0
                                |> Option.exists (fun arg ->
                                    match arg.value with
                                    | BooleanValue includ -> not includ
                                    | _ -> true)
                            | directive when directive.name = "@skip" ->
                                directive.arguments 
                                |> List.tryItem 0
                                |> Option.exists (fun arg ->
                                    match arg.value with
                                    | BooleanValue skip -> skip
                                    | _ -> false)
                            | _ -> false
                        )
                )
            InlineFragmentNode(typeCondition, directives, prunedSelections)

    let rec pruneDirectives (op: GraphQLOp<'a>): GraphQLOp<'a> =
        
        let rec filterSelections (selections: GraphQLOp<'a> list): GraphQLOp<'a> list =
            selections |> List.collect (fun selection -> 
                match selection with 
                | Field(name, args, alias, directives, selections, next) ->
                    let (structure, others) = 
                        directives |> List.partition (fun d -> ["@include"; "@skip"] |> List.contains d.name)

                    let skipSelection = 
                        not (structure |> List.isEmpty) &&
                        structure |> List.forall(function
                            | directive when directive.name = "@include" -> 
                                directive.arguments 
                                |> List.tryItem 0
                                |> Option.exists (fun arg ->
                                    match arg.value with
                                    | BooleanValue includ -> not includ
                                    | _ -> true)
                            | directive when directive.name = "@skip" ->
                                directive.arguments 
                                |> List.tryItem 0
                                |> Option.exists (fun arg ->
                                    match arg.value with
                                    | BooleanValue skip -> skip
                                    | _ -> false)
                            | _ -> false
                        )

                    if skipSelection then [] else [Field(name, args, alias, others, filterSelections selections, next)]
                | InlineFragment(typeCondition, directives, selections, next) as t -> [t] 
            )

        match op with 
        | Field(name, args, alias, directives, selections, next) ->
            let filterNodes = filterSelections selections
            Field(name, args, alias, directives, filterNodes, next)
        | InlineFragment(typeCondition, directives, selections, next) ->
            InlineFragment(typeCondition, directives, selections, next)

    let rec flatten (targetType: Type) (inspector: TypeInspector) (op: GraphQLOp<'a>): GraphQLOp<'a> =

        let rec flattenSelections currentType (selections: GraphQLOp<'a> list) =
            selections 
            |> List.collect (fun selection ->
                match flatten currentType inspector selection with
                | InlineFragment(typeCondition, fragDirectives, fragSelections, fragNext) ->
                    match typeCondition with
                    | Some conditionType ->
                        if inspector.IsSubtypeOf currentType conditionType then
                            flattenSelections currentType fragSelections
                        else
                            []
                    | None ->
                        flattenSelections currentType fragSelections
                | other -> [other]
            )

        match op with
        | Field(name, args, alias, directives, selections, next) ->
            let prop = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            let propType = if prop <> null then prop.PropertyType else targetType
            
            let flattenedSelections = flattenSelections propType selections
            Field(name, args, alias, directives, flattenedSelections, next)
        | InlineFragment(typeCondition, directives, selections, next) ->
            match typeCondition with
                | Some conditionType ->
                    if inspector.IsSubtypeOf targetType conditionType then
                        let flattenedSelections = flattenSelections targetType selections
                        InlineFragment(typeCondition, directives, flattenedSelections, next)
                    else
                        InlineFragment(typeCondition, directives, [], next)
                | None ->
                    let flattenedSelections = flattenSelections targetType selections
                    InlineFragment(typeCondition, directives, flattenedSelections, next)

    