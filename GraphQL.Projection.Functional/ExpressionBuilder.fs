module ExpressionBuilder

open System
open System.Linq
open System.Reflection
open System.Linq.Expressions
open GraphQLSystem
open TypeSystem
open AnonymousTypeBuilder

type ArgumentProcessor = ArgumentNode list -> Expression -> Expression
type DirectiveProcessor = DirectiveNode list -> Expression -> Expression

type Processors = {
    processArguments: ArgumentProcessor
    processDirectives: DirectiveProcessor
}

type BuilderContext = {
    typeInspector: TypeInspector
    typeFactory: AnonymousTypeFactory
    processors: Processors
}
type Builder<'a> = Func<IQueryable<'a>, IQueryable<obj>>

let createExpressionBuilder (ctx: BuilderContext) (processors: Processors) =
    null

let processArgs (args: ArgumentNode list) (expression: Expression): Expression =

    let isEmpty (expr: Expression): bool = (expr = null || expr :? DefaultExpression)

    let isCollectionOperator (op: string): bool = op = "none" || op = "every" || op = "some"

    let processPrimitive (nodeName: string) (nodeValue: obj) (propAccess: Expression) =
        let value = Expression.Constant(nodeValue)
        match nodeName with 
        | "eq" -> Expression.Equal(propAccess, value) :> Expression
        | "ne" -> Expression.NotEqual(propAccess, value) :> Expression
        | "gt" -> Expression.GreaterThan(propAccess, value) :> Expression
        | "gte" -> Expression.GreaterThanOrEqual(propAccess, value) :> Expression
        | "lt" -> Expression.LessThan(propAccess, value) :> Expression
        | "lte" -> Expression.LessThanOrEqual(propAccess, value) :> Expression
        | "contains" | "startsWith" | "endsWith" as collectionOp -> 
            let propType = propAccess.Type
            let containsMethod = 
                propType.GetMethods()
                |> Array.find (fun m -> 
                    m.Name.Equals(collectionOp, StringComparison.OrdinalIgnoreCase) && 
                    m.GetParameters().Length = 1 && m.GetParameters() |> Array.forall (fun p -> p.ParameterType = typeof<string>))
            Expression.Call(propAccess, containsMethod, value)
        //| "in" -> Expression.Empty() :> Expression
        //| "nin" -> Expression.Empty() :> Expression
        | _ -> 
            let property = propAccess.Type.GetProperty(nodeName, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            let access = Expression.Property(propAccess, property)
            Expression.Equal(access, value) :> Expression

    let rec buildPredicate (nodeName: string) (nodeValue: ValueNode) (propAccess: Expression) =
        match nodeValue with 
            | StringValue strValue -> processPrimitive nodeName strValue propAccess
            | IntValue intValue -> processPrimitive nodeName intValue propAccess
            | BooleanValue boolValue -> processPrimitive nodeName boolValue propAccess
            | EnumValue enumValue -> processPrimitive nodeName enumValue propAccess
            | NullValue -> processPrimitive nodeName null propAccess
            //| Variable variable -> processPrimitive nodeName enumValue propAccess
            | ListValue listValue ->
                listValue
                |> List.fold 
                    (fun acc listNode -> 
                        let predicate = buildPredicate String.Empty listNode propAccess
                        match nodeName with
                        | "AND" -> 
                            if isEmpty acc then
                                predicate
                            else 
                                Expression.AndAlso(acc, predicate)
                        | "OR" -> 
                            if isEmpty acc then
                                predicate
                            else 
                                Expression.OrElse(acc, predicate)
                        | _ -> acc
                    ) (Expression.Empty() :> Expression)
            | ObjectValue objectValue ->                   
                let propType = 
                    if defaultInspector.isCollection propAccess.Type then
                        defaultInspector.getElementType propAccess.Type 
                    else Some propAccess.Type
                    |> Option.get

                let property = propType.GetProperty(nodeName, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                let propAccessChild = if isNull property then propAccess else Expression.Property(propAccess, property)                    

                let propeprtyParam = 
                    if defaultInspector.isCollection propAccessChild.Type && isCollectionOperator nodeName then
                        let element = 
                            defaultInspector.getElementType propAccessChild.Type 
                            |> Option.get
                        Expression.Parameter(element) :> Expression
                    else propAccessChild

                let childPredicate = 
                    objectValue
                    |> List.fold 
                        (fun acc objectValueNode -> 
                            let (ovnName, ovnValue) = objectValueNode
                            let predicate = buildPredicate ovnName ovnValue propeprtyParam
                            if isEmpty acc then
                                predicate
                            else 
                                Expression.AndAlso(acc, predicate)
                        ) (Expression.Empty() :> Expression)

                match nodeName with
                | "some" -> 
                    let collectionType = defaultInspector.getCollectionType propAccessChild.Type
                    let allMethod = 
                       collectionType.Value.GetMethods()
                       |> Array.find (fun m -> 
                           m.Name = "Any" && 
                           m.GetParameters().Length = 2)
                    let prodType = 
                        defaultInspector.getElementType propAccessChild.Type 
                        |> Option.get
                    let allMethod = allMethod.MakeGenericMethod(prodType)
                    let lambda = Expression.Lambda(childPredicate, propeprtyParam :?> ParameterExpression)
                    Expression.Call(allMethod, propAccessChild, lambda)

                | "every" -> 
                    let collectionType = defaultInspector.getCollectionType propAccessChild.Type
                    let allMethod = 
                       collectionType.Value.GetMethods()
                       |> Array.find (fun m -> 
                           m.Name = "All" && 
                           m.GetParameters().Length = 2)
                    let prodType = 
                        defaultInspector.getElementType propAccessChild.Type 
                        |> Option.get
                    let allMethod = allMethod.MakeGenericMethod(prodType)
                    let lambda = Expression.Lambda(childPredicate, propeprtyParam :?> ParameterExpression)
                    Expression.Call(allMethod, propAccessChild, lambda)

                | "none" -> 
                    let collectionType = defaultInspector.getCollectionType propAccessChild.Type
                    let allMethod = 
                       collectionType.Value.GetMethods()
                       |> Array.find (fun m -> 
                           m.Name = "Any" && 
                           m.GetParameters().Length = 2)
                    let prodType = 
                        defaultInspector.getElementType propAccessChild.Type 
                        |> Option.get
                    let allMethod = allMethod.MakeGenericMethod(prodType)
                    let lambda = Expression.Lambda(childPredicate, propeprtyParam :?> ParameterExpression)
                    let call = Expression.Call(allMethod, propAccessChild, lambda)
                    Expression.Not(call)
                | _ -> 
                    childPredicate
            | _ -> propAccess

    let processFilter (filterValue: ValueNode) (expression: Expression): Expression = 
        let objectType = 
            defaultInspector.getElementType expression.Type
            |> Option.get

        let parameter = Expression.Parameter(objectType)

        let collectionType = defaultInspector.getCollectionType expression.Type 

        let whereMethod = 
            collectionType.Value.GetMethods()
            |> Array.find (fun m -> 
                m.Name = "Where" && 
                m.GetParameters().Length = 2)

        let whereMethod = whereMethod.MakeGenericMethod(objectType)

        let predicate = buildPredicate String.Empty filterValue parameter
            
        let lambda = Expression.Lambda(predicate, parameter)
        Expression.Call(whereMethod, expression, lambda)

    args
    |> List.fold 
        (fun acc arg -> 
            let name = arg.name
            if name = "filter" then
                let value = arg.value
                processFilter value acc
            else
                acc
        ) expression

let rec toExpression (ctx: BuilderContext) (currentType: Type) (param: Expression) (node: GraphQLNode): Result<Expression, string> = 
    match node with
        | FieldNode(name, _, _, _, selections) when List.isEmpty selections ->
            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            match property with 
            | null -> Error $"Property '{name}' not found on type '{currentType.Name}'"
            | property -> Ok(Expression.Property(param, property))

        | FieldNode(name, args, _, directives, selections) -> 
            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            let propertyAccess = 
                match property with 
                | null -> param
                | property -> Expression.Property(param, property)
                |> ctx.processors.processArguments args
                //|> ctx.processors.processDirectives directives

            let objectType = 
                if ctx.typeInspector.isCollection propertyAccess.Type then 
                    ctx.typeInspector.getElementType propertyAccess.Type
                else Some propertyAccess.Type
                |> Option.get
                            
            let properties = getPropertiesTypes ctx.typeInspector selections objectType
            let anonType = ctx.typeFactory properties
            let ctor = anonType.GetConstructors().[0]
            let typeProperties = anonType.GetProperties()
            let subParameter = Expression.Parameter(objectType)

            let bindings = 
                selections 
                |> Result.traverse (fun selection ->
                    toExpression ctx objectType subParameter selection
                )
                |> Result.map(fun exprList ->
                    exprList
                    |> List.mapi (fun index exp ->
                        Expression.Bind(typeProperties[index], exp) :> MemberBinding
                    )
                )
                
            match bindings with
                | Ok bindings ->  
                    let memberInit = Expression.MemberInit(Expression.New(ctor), bindings) :> Expression

                    if ctx.typeInspector.isCollection propertyAccess.Type
                    then 
                        let collectionType = ctx.typeInspector.getCollectionType propertyAccess.Type 

                        let selectMethod = 
                            collectionType.Value.GetMethods()
                            |> Array.find (fun m -> 
                                m.Name = "Select" && 
                                m.GetParameters().Length = 2)
    
                        let selectMethod = selectMethod.MakeGenericMethod(objectType, memberInit.Type)
    
                        let lambda = Expression.Lambda(memberInit, subParameter)

                        Ok (Expression.Call(selectMethod, propertyAccess, lambda))
                    else Ok memberInit
                | Error err -> Error err
        | _ -> 
            Error ""
    
let builderFactory<'a> (ctx: BuilderContext) (node: GraphQLNode): Builder<'a> =
    let parameter = Expression.Parameter(typeof<IQueryable<'a>>)

    let result = toExpression ctx typeof<'a> parameter node
    
    match result with 
        | Ok body -> Expression.Lambda<Builder<'a>>(body, parameter).Compile()
        | Error err -> failwith(err)
