using System.Linq.Expressions;

namespace GraphQL.Projection.Abstractions.Translation;

public interface IFilterVisitor<TEntity> where TEntity : class
{
    Expression<Func<TEntity, bool>>? Result { get; }
}
