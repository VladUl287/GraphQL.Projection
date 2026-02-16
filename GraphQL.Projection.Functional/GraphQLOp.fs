module GraphQLOp

open System
open TypeSystem
open GraphQLProcessing
open System.Linq.Expressions
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

    let rec prune (node: GraphQLNode) : GraphQLNode =

        let evaluateDirectives (directives: DirectiveNode list) =
            let conditionalDirectives, otherDirectives = 
                directives |> List.partition (fun d -> 
                    d.name = "@include" || d.name = "@skip")
            
            let shouldSkip =
                conditionalDirectives.Length > 0 &&
                conditionalDirectives |> List.forall (fun directive ->
                    match directive.arguments |> List.tryHead with
                    | Some arg ->
                        match directive.name, arg.value with
                        | "@include", BooleanValue false -> true
                        | "@skip", BooleanValue true -> true    
                        | "@include", BooleanValue true -> false
                        | "@skip", BooleanValue false -> false  
                        | _ -> false
                    | None -> false)  
            
            (shouldSkip, otherDirectives)
    
        let processSelections selections =
            selections
            |> List.choose (fun selection ->
                match selection with
                | FieldNode(name, args, alias, directives, subSelections) ->
                    let (shouldSkip, filteredDirectives) = evaluateDirectives directives
                    if shouldSkip 
                    then None 
                    else Some (FieldNode(name, args, alias, filteredDirectives, subSelections))
                        
                | InlineFragmentNode(typeCondition, directives, subSelections) ->
                    let (shouldSkip, filteredDirectives) = evaluateDirectives directives
                    if shouldSkip 
                    then None 
                    else Some (InlineFragmentNode(typeCondition, filteredDirectives, subSelections))
            )
        
        match node with 
        | FieldNode(name, args, alias, directives, selections) ->
            FieldNode(name, args, alias, directives, processSelections selections)

        | InlineFragmentNode(typeCondition, directives, selections) ->
            InlineFragmentNode(typeCondition, directives, processSelections selections)

    let rec flatten (targetType: Type) (inspector: TypeInspector) (node: GraphQLNode): GraphQLNode =

        let rec processSelections (targetType: Type) (inspector: TypeInspector)  (selections: GraphQLNode list) =
            selections 
            |> List.collect (fun selection ->
                match selection with
                | InlineFragmentNode(typeCondition, _, selections) ->
                    match typeCondition with
                    | Some conditionType ->
                        if inspector.IsSubtypeOf targetType conditionType then
                            selections
                        else
                            []
                    | None ->
                        selections
                | other -> [other]
            )

        match node with
        | FieldNode(name, args, alias, directives, selections) ->
            let processedSelections = processSelections targetType inspector selections
            FieldNode(name, args, alias, directives, processedSelections)
        | InlineFragmentNode(typeCondition, directives, selections) -> 
            let processedSelections = processSelections targetType inspector selections
            InlineFragmentNode(typeCondition, directives, processedSelections)

type GraphQLNormalizer = GraphQLOp<GraphQLNode> -> GraphQLOp<GraphQLNode>

type GraphQLOperations = {
    Normilize: GraphQLNormalizer
    Interpret: GraphQLOp<GraphQLNode> -> GraphQLNode
}

