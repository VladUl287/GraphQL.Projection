using System.Reflection;
using System.Linq.Expressions;
using GraphQL.Projection.Visitor;
using GraphQL.Projection.Strategy.Binding.Contracts;

namespace GraphQL.Projection.Strategy.Binding;

public sealed class EntityStrategy(ParameterReplacerFabric parameterReplacerFabric) : IBindingStrategy
{
    public bool AppliesTo(Type type) => type.IsClass && type != typeof(string);

    public MemberAssignment Bind(Expression paramter, MemberInfo property, MemberInitExpression memberInit)
    {
        var paramResolver = new ParameterExpressionVisitor();
        paramResolver.Visit(memberInit);
        var paramExpr = paramResolver.FoundParameter;

        var replacer = parameterReplacerFabric.CreateParameterReplacer(paramExpr, paramter);
        memberInit = (MemberInitExpression)replacer.Visit(memberInit);

        return Expression.Bind(property, memberInit);
    }
}