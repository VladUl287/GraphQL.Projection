module ExpressionBuilder

open System
open GraphQLOp
open GraphQLProcessing
open System.Linq.Expressions
open System.Reflection

type ExpressionBuilderContext = {
    TypeInspector: TypeSystem.TypeInspector
    NodeProcessor: GraphQLProcessing.NodeProcessor
    GraphQLOperations: GraphQLOp.GraphQLOpOperations
    CreateAnonymousType: AnonymousTypeBuilder.Builder
}

module ExpressionBuilder =
    let rec private toExpression (context: ExpressionBuilderContext) (targetType: Type) (param: Expression) (node: GraphQLNode): Expression = 
         match node with
            | FieldNode(name, _, _, _, selections) when List.isEmpty selections ->
                let property = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                Expression.Property(param, property) :> Expression
            | FieldNode(name, args, alias, directives, selections) -> 
                let property = targetType.GetProperty(name, BindingFlags.IgnoreCase ||| BindingFlags.Public ||| BindingFlags.Instance)
                let accessExpr = 
                    if isNull property then param 
                    else Expression.Property(param, property)
                let accessType = accessExpr.Type
                    
                if TypeSystem.defaultInspector.IsCollection accessType then 
                    let elementType = (TypeSystem.defaultInspector.GetElementType accessType).Value

                    let properties = getPropertiesTypes TypeSystem.defaultInspector selections elementType
                    
                    let anonType = context.CreateAnonymousType.CreateType properties
                    
                    let ctor = anonType.GetConstructors().[0]
                    
                    let subParameter = Expression.Parameter(elementType)

                    let members = 
                        selections 
                        |> List.map (fun selection ->
                            toExpression context elementType subParameter selection
                        )
                                        
                    let bindings = 
                        members
                        |> List.mapi (fun index exp ->
                            Expression.Bind(anonType.GetProperties()[index], exp) :> MemberBinding
                        )

                    let memberInit = Expression.MemberInit(Expression.New(ctor), bindings)

                    let selectMethod = 
                        typeof<System.Linq.Enumerable>.GetMethods()
                        |> Array.find (fun m -> 
                            m.Name = "Select" && 
                            m.GetParameters().Length = 2)

                    let genericSelectMethod = selectMethod.MakeGenericMethod(elementType, memberInit.Type)

                    let lambda = Expression.Lambda(memberInit, subParameter)

                    Expression.Call(genericSelectMethod, accessExpr, lambda)
                else
                    let properties = getPropertiesTypes TypeSystem.defaultInspector selections accessType

                    let anonType = context.CreateAnonymousType.CreateType properties
                    
                    let ctor = anonType.GetConstructors().[0]
                    
                    let members = 
                        selections 
                        |> List.map (fun selection ->
                            toExpression context accessType accessExpr selection
                        )
                                        
                    let bindings = 
                        members
                        |> List.mapi (fun index exp ->
                            Expression.Bind(anonType.GetProperties()[index], exp) :> MemberBinding
                        )

                    Expression.MemberInit(Expression.New(ctor), bindings) :> Expression
            | InlineFragmentNode(_, _, _) -> 
               Expression.Empty()

    let buildSelector<'a> (context: ExpressionBuilderContext) (query: GraphQLOp<GraphQLNode>) : Expression<Func<'a, obj>> =
        let processedQuery = 
            query 
            |> Operations.map context.GraphQLOperations.Prune
            |> Operations.map context.GraphQLOperations.Flatten
    
        let node = context.GraphQLOperations.Interpret processedQuery
    
        let parameter = Expression.Parameter(typeof<'a>)
        let body = toExpression context typeof<'a> parameter node

        Expression.Lambda<Func<'a, obj>>(body, parameter)