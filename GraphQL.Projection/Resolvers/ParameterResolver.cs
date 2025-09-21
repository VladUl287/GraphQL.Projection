using System.Linq.Expressions;
using GraphQL.Projection.Resolvers.Contracts;
using GraphQL.Projection.Visitor;

namespace GraphQL.Projection.Resolvers;

public sealed class ParameterResolver : IParameterResolver
{
    public ParameterExpression GetParameterExpression(MemberInitExpression memberInit)
    {
        var visitor = new ParameterExpressionVisitor();
        visitor.Visit(memberInit);
        return visitor.FoundParameter ?? throw new InvalidOperationException("Parameter expression not found.");
    }
}
