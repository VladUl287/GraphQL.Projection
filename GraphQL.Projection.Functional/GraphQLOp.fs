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

    let rec pruneDirectives (queryRoot: GraphQLOp<'a>) (op: GraphQLOp<'a>): GraphQLOp<'a> =

        let getValue (queryRoot: GraphQLOp<'a>) (node: DirectiveNode) =
            
            1

        let rec prune (selections: GraphQLOp<'a> list): GraphQLOp<'a> list =
           selections 
           |> List.collect (fun selection ->
               let directives = 
                    match selection with
                    | Field(_, _, _, directives, _, _) -> directives
                    | InlineFragment(_, directives, _, _) -> directives
                    |> List.filter(fun directive ->
                        ["@include"; "@skip"] 
                        |> List.contains directive.name
                    )
               []
           )

        match op with
        | Field(_, _, _, directives, _, _) as field when directives |> List.isEmpty -> field
        | other -> other

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

    