using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.QueryBuilding;

public interface IFilterApplier<TEntity>
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, IEnumerable<ASTNode> filters);
}
