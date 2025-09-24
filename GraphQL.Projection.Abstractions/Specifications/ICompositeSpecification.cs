namespace GraphQL.Projection.Abstractions.Specifications;

public interface ICompositeSpecification<TEntity> : ISpecification<TEntity>
{
    ISpecification<TEntity> Left { get; }
    ISpecification<TEntity> Right { get; }
}
