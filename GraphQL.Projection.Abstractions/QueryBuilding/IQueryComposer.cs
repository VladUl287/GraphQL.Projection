using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.QueryBuilding;

public interface IQueryComposer<TEntity>
{
    IQueryable<TEntity> Compose(IQueryable<TEntity> source, ASTNode node);
}
