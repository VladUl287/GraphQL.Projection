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

type ArgumentProcessor = BuilderContext -> Type -> Expression -> GraphQLNode -> Expression option

let rec toExpression (ctx: BuilderContext) (currentType: Type) (param: Expression) (node: GraphQLNode): Result<Expression, string> = 
    let getProperty (typ: Type) (name: string) = 
        typ.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)

    match node with
        | FieldNode(name, _, _, _, selections) when List.isEmpty selections ->
            match getProperty currentType name with 
            | null -> Error $"Property '{name}' not found on type '{currentType.Name}'"
            | property -> Ok(Expression.Property(param, property))

        | FieldNode(name, args, _, _, selections) -> 
            let propertyAccess = 
                match getProperty currentType name with 
                | null -> param
                | property -> Expression.Property(param, property)

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
