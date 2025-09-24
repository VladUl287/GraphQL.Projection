using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.Execution;

public interface IProjectionStrategy
{
    IQueryable<TResult> Project<TEntity, TResult>(IQueryable<TEntity> query, ASTNode node);
}
