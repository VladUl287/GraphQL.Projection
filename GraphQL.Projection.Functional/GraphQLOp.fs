module GraphQLOp

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
    
    let inlineFragment' typeCondition directives selections = 
        InlineFragment(typeCondition, directives, selections, fun node -> node)
    
    let rec interpret (op: GraphQLOp<GraphQLNode>) : GraphQLNode =
        match op with
        | Field(name, args, alias, directives, selections, next) -> 
            let nodes = selections |> List.map interpret
            next (FieldNode(name, args, alias, directives, nodes))
        | InlineFragment(typeCondition, directives, selections, next) -> 
            let nodes = selections |> List.map interpret
            next (InlineFragmentNode(typeCondition, directives, nodes))
    