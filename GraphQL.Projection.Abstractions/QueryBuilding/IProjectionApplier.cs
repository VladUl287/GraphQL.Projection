using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.QueryBuilding;

public interface IProjectionApplier
{
    IQueryable<TResult> Apply<TEntity, TResult>(IQueryable<TEntity> query, ASTNode node);
}
