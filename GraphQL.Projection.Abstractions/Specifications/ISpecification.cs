using System.Linq.Expressions;

namespace GraphQL.Projection.Abstractions.Specifications;

public interface ISpecification<TEntity>
{
    Expression<Func<TEntity, bool>> ToExpression();

    bool IsSatisfiedBy(TEntity entity);
}
