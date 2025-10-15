using System.Linq.Expressions;

namespace GraphQL.Projection.Models;

public sealed record QueryModel(LambdaExpression Select)
{
    public static readonly QueryModel Empty = new((LambdaExpression)null);
}
