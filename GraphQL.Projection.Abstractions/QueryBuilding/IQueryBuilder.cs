using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.QueryBuilding;

public interface IQueryBuilder<TEntity>
{
    IQueryable<TEntity> Build(IQueryable<TEntity> source, ASTNode node);
}
