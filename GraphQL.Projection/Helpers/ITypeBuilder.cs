using GraphQLParser.AST;
using System.Linq.Expressions;

namespace GraphQL.Projection.Helpers;

public interface ITypeBuilder
{
    MemberInitExpression BuildType(Type type, GraphQLSelectionSet selectionSet);
}
