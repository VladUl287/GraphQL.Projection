using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.QueryBuilding;

public interface ISortApplier<TEntity>
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, IEnumerable<ASTNode> sorts);
}
