module FilterBuilder

open TypeSystem
open GraphQLSystem
open System.Linq.Expressions

type BuildPredicate = TypeInspector -> Expression -> ValueNode -> Expression

let rec build (inspector: TypeInspector) (param: Expression) (node: ValueNode): Expression =
    match node with
    | StringValue str -> buildComparison inspector param str
    | IntValue i -> buildComparison inspector param i
    | BooleanValue b -> buildComparison inspector param b
    | EnumValue e -> buildComparison inspector param e
    | NullValue -> buildComparison inspector param null
    | ObjectValue obj -> buildObject inspector param obj
    | ListValue lst -> buildList inspector param lst
    | _ -> param

and private buildObject (inspector: TypeInspector) (param: Expression) (objNodes: (string * ValueNode) list) =
    let parameterType = 
        if inspector.isCollection param.Type then
            param.Type 
            |> inspector.getElementType
            |> Option.defaultValue param.Type
        else param.Type

    let predicate = 
        objNodes

    (Expression.Empty() :> Expression)

and private buildList inspector param listNodes =
    (Expression.Empty() :> Expression)

and private buildComparison (inspector: TypeInspector) (param: Expression) (value: obj): Expression =
    (Expression.Empty() :> Expression)