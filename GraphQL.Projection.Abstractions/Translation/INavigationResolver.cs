using GraphQLParser.AST;

namespace GraphQL.Projection.Abstractions.Translation;

public interface INavigationResolver
{
    IEnumerable<string> ResolveIncludes(ASTNode node);
}
