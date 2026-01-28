module GraphQLOp

type GraphQLNode =
    | FieldNode of name: string * arguments: ArgumentNode list
    | ObjectNode of name: string * selections: GraphQLNode list

and ArgumentNode = {
    Name: string
    Value: GraphQLValue
}

and GraphQLValue =
    | StringValue of string
    | IntValue of int
    | FloatValue of float
    | BooleanValue of bool
    | NullValue
    | VariableValue of name: string
    | ListValue of GraphQLValue list
    | ObjectValue of Map<string, GraphQLValue>

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
    