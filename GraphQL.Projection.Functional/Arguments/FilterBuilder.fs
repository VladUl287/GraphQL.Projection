module FilterBuilder

open TypeSystem
open GraphQLSystem
open System.Linq.Expressions
open System
open System.Reflection

type BuildPredicate = TypeInspector -> Expression -> ValueNode -> Expression

let isEmpty (expr: Expression): bool = (expr = null || expr :? DefaultExpression)

let rec build (inspector: TypeInspector) (param: Expression) (node: (string * ValueNode)): Expression =
    match node with
    | (op, primitiveNode) when isPrimitiveOp op -> 
        let value = 
            match primitiveNode with
            | StringValue s -> box s
            | IntValue i -> box i
            | BooleanValue b -> box b
            | EnumValue e -> box e
            | NullValue -> null
            | _ -> failwith "Unexpected node type"
        buildPrimitiveOp param (op, value)

    | (("AND" | "OR") as op, ListValue lst) -> 
        lst 
        |> List.fold
            (fun acc item ->
                let predicate = build inspector param ("ROOT", item)
                match op with 
                | "AND" -> 
                    if isEmpty acc then predicate else Expression.AndAlso(acc, predicate)
                | "OR" -> 
                    if isEmpty acc then predicate else Expression.OrElse(acc, predicate)
                | _ -> acc
            ) (Expression.Empty())
    
    | (("SOME" | "EVERY" | "NONE"), ObjectValue obj) -> param

    | ("ROOT", ObjectValue obj) -> 
        obj
        |> List.map (build inspector param)
        |> List.fold 
            (fun acc expr -> 
                if acc :? DefaultExpression then expr
                else Expression.AndAlso(acc, expr)
            ) (Expression.Empty())

    | (property, StringValue v) -> 
        let propertyInfo = param.Type.GetProperty(property, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance) 
        let propertyAccess = Expression.Property(param, propertyInfo)
        buildPrimitiveOp propertyAccess ("eq", v)

    | (property, ObjectValue obj) -> 
        let propertyInfo = param.Type.GetProperty(property, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance) 
        let propertyAccess = Expression.Property(param, propertyInfo)
        obj
        |> List.map (build inspector propertyAccess)
        |> List.fold 
            (fun acc expr -> 
                if acc :? DefaultExpression then expr
                else Expression.AndAlso(acc, expr)
            ) (Expression.Empty())

    //| ObjectValue obj when isPrimitiveOpObject obj -> buildPrimitiveOp propAccess obj

    //| ObjectValue obj -> 
    //        obj
    //        |> List.map 
    //            (fun node -> 
    //                match node with
    //                | (("AND" | "OR"), ListValue lstValue) as (op, _) -> 
    //                    lstValue 
    //                    |> List.fold
    //                        (fun acc lstNode ->
    //                            let childPredicate = build inspector propAccess lstNode
    //                            match op with 
    //                                | "AND" -> Expression.AndAlso(acc, childPredicate) :> Expression
    //                                | "OR" -> Expression.OrElse(acc, childPredicate)
    //                                | _ -> acc
    //                        ) propAccess
    //                | (propName, propValue) -> 
    //                    let childProp = Expression.Property(propAccess, propName)
    //                    build inspector childProp propValue
    //            )
    //        |> List.fold 
    //            (fun acc expr -> 
    //                //if acc :? DefaultExpression then expr
    //                if isEmpty acc then expr
    //                else Expression.AndAlso(acc, expr) :> Expression
    //            ) (Expression.Empty() :> Expression)

    //| ListValue lst -> 
    //    lst
    //    |> List.map (build inspector propAccess)
    //    propAccess

    | _ -> param

and private isPrimitiveOp (key: string): bool =
    match key with
        | "eq" | "ne" | "gt" | "gte" | "lt" | "lte"
        | "contains" | "startsWith" | "endsWith" 
        | "in" | "nin" -> true
        | _ -> false

and private buildPrimitiveOp (propAccess: Expression) ((op, value): string * objnull): Expression =
    let exprValue = Expression.Constant(value)
    match op with 
    | "eq" -> Expression.Equal(propAccess, exprValue)
    | "ne" -> Expression.NotEqual(propAccess, exprValue)
    | "gt" -> Expression.GreaterThan(propAccess, exprValue)
    | "gte" -> Expression.GreaterThanOrEqual(propAccess, exprValue)
    | "lt" -> Expression.LessThan(propAccess, exprValue)
    | "lte" -> Expression.LessThanOrEqual(propAccess, exprValue)
    | "contains" | "startsWith" | "endsWith" as collectionOp -> 
        let containsMethod = 
            propAccess.Type.GetMethods()
            |> Array.find (fun m -> 
                m.Name.Equals(collectionOp, StringComparison.OrdinalIgnoreCase) && 
                m.GetParameters().Length = 1 && m.GetParameters() |> Array.forall (fun p -> p.ParameterType = typeof<string>))
        Expression.Call(propAccess, containsMethod, exprValue)
    | "in" | "nin" -> propAccess
    | _ -> propAccess