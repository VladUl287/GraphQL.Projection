namespace GraphQL.Projection.Abstractions.Execution;

public interface IQueryExecutor<TEntity>
{
    IQueryable<TEntity> Execute(IQueryable<TEntity> source, string query);
}

