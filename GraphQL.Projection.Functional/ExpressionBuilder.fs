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
    //args
    //    |> List.iter (fun item -> 
    //        let name = item.name

    //        if name = "filter" then
    //            let value = item.value
    //            ()
    //        elif name = "sort" then
    //            let value = item.value
    //            ()
    //        elif name = "pagination" then
    //            let value = item.value
    //            ()
    //        else
    //            ()
    //        )

    args
    |> List.fold 
        (fun acc arg -> 
            let name = arg.name

            if name = "filter" then
                let value = arg.value
                match value with 
                | ObjectValue filterValue -> 
                    filterValue
                    |> List.fold 
                        (fun filterAcc filterValue -> 
                            let (filterName, valueNode) = filterValue
                            let property = expression.Type.GetProperty(filterName, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                            if isNull property then
                                filterAcc
                            else
                                
                                filterAcc
                        ) acc
                | _ -> acc
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
                |> ctx.processors.processDirectives directives

            let objectType = 
                if ctx.typeInspector.isCollection propertyAccess.Type then 
                    ctx.typeInspector.getElementType propertyAccess.Type
                else Some propertyAccess.Type
                |> Option.get
                            
            let properties = getPropertiesTypes ctx.typeInspector selections objectType
            let anonType = ctx.typeFactory properties
            let ctor = anonType.GetConstructors().[0]
            let typeProperties = anonType.GetProperties()

            let bindings = 
                selections 
                |> Result.traverse (fun selection ->
                    toExpression ctx objectType propertyAccess selection
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
                        let subParameter = Expression.Parameter(objectType)

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
