using System.Linq.Expressions;

namespace GraphQL.Projection.Models;

public sealed record QueryModel<TEntity>(Expression<Func<TEntity, TEntity>> Select)
{
    public static readonly QueryModel<TEntity> Empty = new((Expression<Func<TEntity, TEntity>>)(null));
}
