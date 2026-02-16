module ExpressionSystem

open System
open GraphQLProcessing
open System.Linq.Expressions
open System.Linq
open System.Reflection
open AnonymousTypeBuilder

type BuilderFactory<'a> = {
    Create: GraphQLNode -> Func<IQueryable<'a>, IQueryable<obj>>
}

type ExpressionBuilderContext = {
    TypeInspector: TypeSystem.TypeInspector
    NodeProcessor: GraphQLProcessing.NodeProcessor
    AnonymousTypeFactory: AnonymousTypeBuilder.AnonymousTypeFactory
}

let rec toExpression (currentType: Type) (param: Expression) (node: GraphQLNode): Expression = 
    match node with
        | FieldNode(name, _, _, _, selections) when List.isEmpty selections ->
            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            Expression.Property(param, property) :> Expression
        | FieldNode(name, args, alias, directives, selections) -> 
            let property = currentType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
            let accessExpr = 
                if isNull property then param 
                else Expression.Property(param, property)
            let accessType = accessExpr.Type
                
            if TypeSystem.defaultInspector.IsCollection accessType then 
                let elementType = (TypeSystem.defaultInspector.GetElementType accessType).Value
    
                let properties = getPropertiesTypes TypeSystem.defaultInspector selections elementType
                
                let anonType = createAnonymousType properties
                
                let ctor = anonType.GetConstructors().[0]
                
                let subParameter = Expression.Parameter(elementType)
    
                let members = 
                    selections 
                    |> List.map (fun selection ->
                        toExpression elementType subParameter selection
                    )
    
                
                let bindings = 
                    members
                    |> List.mapi (fun index exp ->
                        Expression.Bind(anonType.GetProperties()[index], exp) :> MemberBinding
                    )
    
                let memberInit = Expression.MemberInit(Expression.New(ctor), bindings)
    
                let collectionType = TypeSystem.defaultInspector.GetCollectionType accessType

                let selectMethod = 
                    collectionType.Value.GetMethods()
                    |> Array.find (fun m -> 
                        m.Name = "Select" && 
                        m.GetParameters().Length = 2)
    
                let genericSelectMethod = selectMethod.MakeGenericMethod(elementType, memberInit.Type)
    
                let lambda = Expression.Lambda(memberInit, subParameter)
    
                Expression.Call(genericSelectMethod, accessExpr, lambda)
            else
                let properties = getPropertiesTypes TypeSystem.defaultInspector selections accessType
    
                let anonType = createAnonymousType properties
                
                let ctor = anonType.GetConstructors().[0]
                
                let members = 
                    selections 
                    |> List.map (fun selection ->
                        toExpression accessType accessExpr selection
                    )
                                    
                let bindings = 
                    members
                    |> List.mapi (fun index exp ->
                        Expression.Bind(anonType.GetProperties()[index], exp) :> MemberBinding
                    )
    
                Expression.MemberInit(Expression.New(ctor), bindings) :> Expression
        | InlineFragmentNode(_, _, _) -> 
           Expression.Empty()
    
let createFactory<'a> (node: GraphQLNode): Func<IQueryable<'a>, IQueryable<obj>> =
    let parameter = Expression.Parameter(typeof<IQueryable<'a>>)

    let body = toExpression typeof<'a> parameter node
    
    let result = Expression.Lambda<Func<IQueryable<'a>, IQueryable<obj>>>(body, parameter)

    result.Compile()

let defaultFactory: BuilderFactory<'a> = {
    Create = createFactory<'a>
}