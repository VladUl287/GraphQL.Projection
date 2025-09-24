using GraphQLParser.AST;
using System.Linq.Expressions;

namespace GraphQL.Projection.Abstractions.Translation;

public interface IQueryTranslator<TEntity> where TEntity : class
{
    Expression<Func<TEntity, bool>> TranslateFilters(IEnumerable<ASTNode> filters);
    IEnumerable<(LambdaExpression KeySelector, bool Descending)> TranslateSorts(IEnumerable<ASTNode> sorts);
}
