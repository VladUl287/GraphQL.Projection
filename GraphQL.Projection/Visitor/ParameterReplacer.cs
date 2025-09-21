using System.Linq.Expressions;

namespace GraphQL.Projection.Visitors;

public sealed class ParameterReplacer : ExpressionVisitor
{
    private readonly Expression _oldParameter;
    private readonly Expression _newParameter;

    public ParameterReplacer(Expression oldParameter, Expression newParameter)
    {
        _oldParameter = oldParameter;
        _newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node == _oldParameter ? _newParameter : base.VisitParameter(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var expression = Visit(node.Expression);
        return expression != node.Expression ? Expression.MakeMemberAccess(expression, node.Member) : node;
    }

    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        switch (node.BindingType)
        {
            case MemberBindingType.Assignment:
                var assignment = (MemberAssignment)node;
                var expression = Visit(assignment.Expression);
                return expression != assignment.Expression ? Expression.Bind(assignment.Member, expression) : node;
            default:
                return base.VisitMemberBinding(node);
        }
    }
}

