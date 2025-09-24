namespace GraphQL.Projection.Abstractions.Specifications;

public interface IFilterSpecification<TEntity> : ISpecification<TEntity>
{
    string Field { get; }

    string Operator { get; }

    object? Value { get; }
}
