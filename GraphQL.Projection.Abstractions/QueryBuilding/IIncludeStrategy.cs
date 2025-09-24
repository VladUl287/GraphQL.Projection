namespace GraphQL.Projection.Abstractions.QueryBuilding;

public interface IIncludeStrategy
{
    IQueryable<TEntity> ApplyIncludes<TEntity>(IQueryable<TEntity> query, IEnumerable<string> includePaths);
}
