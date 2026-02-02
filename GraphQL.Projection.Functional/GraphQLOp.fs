module GraphQLOp

type GraphQLNode =
   | FieldNode of name: string * arguments: ArgumentNode list
        //alias: string option * 
        //directives: DirectiveNode list * 
        //selections: GraphQLNode list
    
   | ObjectNode of name: string * selections: GraphQLNode list

   | InlineFragment of 
      typeCondition: string option * 
      directives: DirectiveNode list * 
      selections: GraphQLNode list

   | FragmentSpread of 
       name: string * 
       directives: DirectiveNode list

   member this.Name =
       match this with
       | FieldNode(name, _) -> name
       | ObjectNode(name, _) -> name

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
    | Field of name: string * arguments: ArgumentNode list * next: (GraphQLNode -> 'a)
    | Object of name: string * selections: GraphQLOp<'a> list * next: (GraphQLNode -> 'a)

module Operations =
    let rec map (f: 'a -> 'b) (op: GraphQLOp<'a>) : GraphQLOp<'b> =
        match op with
        | Field(name, args, next) -> Field(name, args, next >> f)
        | Object(name, selections, next) -> Object(name, List.map (map f) selections, next >> f)
    
    let field name args = 
        Field(name, args, fun node -> node)
    
    let object' name selections = 
        Object(name, selections, fun node -> node)
    
    let rec interpret (op: GraphQLOp<GraphQLNode>) : GraphQLNode =
        match op with
        | Field(name, args, next) -> next (FieldNode(name, args))
        | Object(name, selections, next) -> 
            let nodes = selections |> List.map interpret
            next (ObjectNode(name, nodes))
    