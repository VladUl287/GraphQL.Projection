using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.QueryBuilding;

public interface IPaginationApplier<TEntity>
{
    IQueryable<TEntity> Apply(IQueryable<TEntity> query, ASTNode node);
}
