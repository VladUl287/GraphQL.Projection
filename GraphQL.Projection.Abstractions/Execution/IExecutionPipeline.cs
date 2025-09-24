using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.Execution;

public interface IExecutionPipeline<TEntity>
{
    IQueryable<TEntity> Execute(IQueryable<TEntity> source, ASTNode node);
}
