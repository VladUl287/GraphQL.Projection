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

let rec toExpression (ctx: BuilderContext) (currentType: Type) (param: Expression) (node: GraphQLNode): Expression = 
    match node with
        | FieldNode(name, _, _, _, selections) when List.isEmpty selections ->
            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            Expression.Property(param, property) :> Expression
        | FieldNode(name, args, alias, directives, selections) -> 
            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            
            let valueAccess = 
                if isNull property then param 
                else Expression.Property(param, property)

            let propertyType = 
                if ctx.typeInspector.isCollection valueAccess.Type 
                then ctx.typeInspector.getElementType valueAccess.Type
                else Some valueAccess.Type
                |> Option.get //
            
            let properties = getPropertiesTypes ctx.typeInspector selections propertyType
    
            let anonType = createAnonymousType properties
            
            let ctor = anonType.GetConstructors().[0]
            
            let members = 
                selections 
                |> List.map (fun selection ->
                    toExpression ctx propertyType valueAccess selection
                )
                                
            let bindings = 
                members
                |> List.mapi (fun index exp ->
                    Expression.Bind(anonType.GetProperties()[index], exp) :> MemberBinding
                )
    
            let memberInit = Expression.MemberInit(Expression.New(ctor), bindings) :> Expression

            if ctx.typeInspector.isCollection valueAccess.Type
            then 
                let subParameter = Expression.Parameter(propertyType)

                let collectionType = ctx.typeInspector.getCollectionType valueAccess.Type 

                let selectMethod = 
                    collectionType.Value.GetMethods()
                    |> Array.find (fun m -> 
                        m.Name = "Select" && 
                        m.GetParameters().Length = 2)
    
                let selectMethod = selectMethod.MakeGenericMethod(propertyType, memberInit.Type)
    
                let lambda = Expression.Lambda(memberInit, subParameter)
    
                Expression.Call(selectMethod, valueAccess, lambda)
            else memberInit
        | _ -> 
            Expression.Empty()
    
let builderFactory<'a> (ctx: BuilderContext) (node: GraphQLNode): Builder<'a> =
    let parameter = Expression.Parameter(typeof<IQueryable<'a>>)

    let body = toExpression ctx typeof<'a> parameter node
    
    let result = Expression.Lambda<Builder<'a>>(body, parameter)

    result.Compile()
