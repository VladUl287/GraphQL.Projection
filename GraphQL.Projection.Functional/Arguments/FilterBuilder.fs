module FilterBuilder

open TypeSystem
open GraphQLSystem
open System.Linq.Expressions
open System

type BuildPredicate = TypeInspector -> Expression -> ValueNode -> Expression

let isEmpty (expr: Expression): bool = (expr = null || expr :? DefaultExpression)

let rec build (inspector: TypeInspector) (propAccess: Expression) (node: ValueNode): Expression =
    match node with
    //| StringValue str -> buildComparison inspector param str
    //| IntValue i -> buildComparison inspector param i
    //| BooleanValue b -> buildComparison inspector param b
    //| EnumValue e -> buildComparison inspector param e
    //| NullValue -> buildComparison inspector param null

    | ObjectValue obj when isPrimitiveOpObject obj -> buildPrimitiveOp propAccess obj

    | ObjectValue obj -> 
            obj
            |> List.map 
                (fun node -> 
                    match node with
                    | (("AND" | "OR"), ListValue lstValue) as (op, _) -> 
                        lstValue 
                        |> List.fold
                            (fun acc lstNode ->
                                let childPredicate = build inspector propAccess lstNode
                                match op with 
                                    | "AND" -> Expression.AndAlso(acc, childPredicate) :> Expression
                                    | "OR" -> Expression.OrElse(acc, childPredicate)
                                    | _ -> acc
                            ) propAccess
                    | (propName, propValue) -> 
                        let childProp = Expression.Property(propAccess, propName)
                        build inspector childProp propValue
                )
            |> List.fold 
                (fun acc expr -> 
                    //if acc :? DefaultExpression then expr
                    if isEmpty acc then expr
                    else Expression.AndAlso(acc, expr) :> Expression
                ) (Expression.Empty() :> Expression)

    //| ListValue lst -> 
    //    lst
    //    |> List.map (build inspector propAccess)
    //    propAccess

    | _ -> propAccess

and private isPrimitiveOpObject (obj: (string * ValueNode) list) =
    obj |> List.exists (fun (key, _) -> 
        match key with
        | "eq" | "ne" | "gt" | "gte" | "lt" | "lte"
        | "contains" | "startsWith" | "endsWith" 
        | "in" | "nin" -> true
        | _ -> false)

and private buildPrimitiveOp (propAccess: Expression) (compareValue: (string * ValueNode) list): Expression =
    let (op, value) = compareValue |> List.exactlyOne

    let value = Expression.Constant(value)

    match op with 
    | "eq" -> Expression.Equal(propAccess, value)
    | "ne" -> Expression.NotEqual(propAccess, value)
    | "gt" -> Expression.GreaterThan(propAccess, value)
    | "gte" -> Expression.GreaterThanOrEqual(propAccess, value)
    | "lt" -> Expression.LessThan(propAccess, value)
    | "lte" -> Expression.LessThanOrEqual(propAccess, value)
    | "contains" | "startsWith" | "endsWith" as collectionOp -> 
        let containsMethod = 
            propAccess.Type.GetMethods()
            |> Array.find (fun m -> 
                m.Name.Equals(collectionOp, StringComparison.OrdinalIgnoreCase) && 
                m.GetParameters().Length = 1 && m.GetParameters() |> Array.forall (fun p -> p.ParameterType = typeof<string>))
        Expression.Call(propAccess, containsMethod, value)
    | "in" | "nin" -> propAccess
    | _ -> propAccess

and private buildLogicalOp (inspector: TypeInspector) (propAccess: Expression) (lstValues: (string * ValueNode) list): Expression =
    let (op, value) = lstValues |> List.exactlyOne
    lstValues 
    |> List.fold
        (fun acc (op, node) ->
            let childPredicate = build inspector propAccess node
            match op with 
            | "AND" -> Expression.AndAlso(acc, childPredicate)
            | "OR" -> Expression.OrElse(acc, childPredicate)
            | _ -> acc
        ) propAccess

and private isLogicalObject (obj: (string * ValueNode) list) =
    obj |> List.exists (fun (key, _) -> key = "AND" || key = "OR")

and private isCollectionOperatorObject (obj: (string * ValueNode) list) =
    obj |> List.exists (fun (key, _) -> key = "some" || key = "every" || key = "none")