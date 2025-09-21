using System.Linq.Expressions;

namespace GraphQL.Projection.Visitor;

public sealed class ParameterExpressionVisitor : ExpressionVisitor
{
    public ParameterExpression? FoundParameter { get; private set; }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is ParameterExpression paramExpr)
        {
            FoundParameter = paramExpr;
        }
        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        foreach (var argument in node.Arguments)
        {
            Visit(argument);
            if (FoundParameter is not null)
            {
                return node;
            }
        }
        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        foreach (var binding in node.Bindings)
        {
            if (binding is MemberAssignment assignment)
            {
                Visit(assignment.Expression);
                if (FoundParameter is not null)
                {
                    return node;
                }
            }
        }
        return base.VisitMemberInit(node);
    }
}
