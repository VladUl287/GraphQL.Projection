using System.Linq.Expressions;

namespace GraphQL.Projection.Abstractions.Translation;

public interface ISortVisitor<TEntity> where TEntity : class
{
    IEnumerable<(LambdaExpression KeySelector, bool Descending)> Results { get; }
}

