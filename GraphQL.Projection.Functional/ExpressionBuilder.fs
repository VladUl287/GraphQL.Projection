module ExpressionBuilder

open System
open System.Linq
open System.Reflection
open System.Linq.Expressions
open GraphQLSystem
open TypeSystem
open AnonymousTypeBuilder

type BuilderContext = {
    typeInspector: TypeInspector
    typeFactory: AnonymousTypeFactory
}
type Builder<'a> = Func<IQueryable<'a>, IQueryable<obj>>

let rec toExpression (ctx: BuilderContext) (currentType: Type) (param: Expression) (node: GraphQLNode): Result<Expression, string> = 
    match node with
        | FieldNode(name, _, _, _, selections) when List.isEmpty selections ->
            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            Ok(Expression.Property(param, property))
        | FieldNode(name, args, _, _, selections) -> 
            let propertyInfo = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            
            let sourceExpr = 
                if isNull propertyInfo then 
                    param 
                else 
                    Expression.Property(param, propertyInfo)

            let underlyingType = 
                if ctx.typeInspector.isCollection sourceExpr.Type 
                then ctx.typeInspector.getElementType sourceExpr.Type
                else Some sourceExpr.Type
                |> Option.get
            
            let properties = getPropertiesTypes ctx.typeInspector selections underlyingType
            let anonType = ctx.typeFactory properties
            let ctor = anonType.GetConstructors().[0]
            let typeProperties = anonType.GetProperties()

            let bindings = 
                selections 
                |> Result.traverse (fun selection ->
                    toExpression ctx underlyingType sourceExpr selection
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

                    if ctx.typeInspector.isCollection sourceExpr.Type
                    then 
                        let subParameter = Expression.Parameter(underlyingType)

                        let collectionType = ctx.typeInspector.getCollectionType sourceExpr.Type 

                        let selectMethod = 
                            collectionType.Value.GetMethods()
                            |> Array.find (fun m -> 
                                m.Name = "Select" && 
                                m.GetParameters().Length = 2)
    
                        let selectMethod = selectMethod.MakeGenericMethod(underlyingType, memberInit.Type)
    
                        let lambda = Expression.Lambda(memberInit, subParameter)
    
                        Ok (Expression.Call(selectMethod, sourceExpr, lambda))
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
