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

    | (op, ObjectValue obj) when isCollectionOperatorObject op -> param

    | (("AND" | "OR") as op, ListValue lst) -> 
        lst 
        |> List.fold
            (fun acc item ->
                let predicate = build inspector param (String.Empty, item)
                match op with 
                | "AND" -> 
                    if isEmpty acc then predicate else Expression.AndAlso(acc, predicate)
                | "OR" -> 
                    if isEmpty acc then predicate else Expression.OrElse(acc, predicate)
                | _ -> acc
            ) (Expression.Empty())

    | (property, ObjectValue obj) -> 
        let propertyInfo = param.Type.GetProperty(property, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance) 
        let propertyAccess = if isNull propertyInfo then param else Expression.Property(param, propertyInfo)
        obj
        |> List.map (build inspector propertyAccess)
        |> List.fold 
            (fun acc expr -> 
                if acc :? DefaultExpression then expr
                else Expression.AndAlso(acc, expr)
            ) (Expression.Empty())

    | (property, ((StringValue _ | EnumValue _ | IntValue _ | BooleanValue _ | NullValue) as primitiveNode)) -> 
        let propertyInfo = param.Type.GetProperty(property, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance) 
        let propertyAccess = Expression.Property(param, propertyInfo)
        let value = 
            match primitiveNode with
            | StringValue s -> box s
            | IntValue i -> box i
            | BooleanValue b -> box b
            | EnumValue e -> box e
            | NullValue -> null
            | _ -> failwith "Unexpected node type"
        buildPrimitiveOp propertyAccess ("eq", value)

    | _ -> param

and private isPrimitiveOp (key: string): bool =
    match key with
        | "eq" | "ne" | "gt" | "gte" | "lt" | "lte"
        | "contains" | "startsWith" | "endsWith" 
        | "in" | "nin" -> true
        | _ -> false

and private isCollectionOperatorObject (key: string): bool = key = "SOME" || key = "EVERY" || key = "NONE"

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