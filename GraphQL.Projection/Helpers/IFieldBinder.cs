using GraphQLParser.AST;
using System.Linq.Expressions;

namespace GraphQL.Projection.Helpers;

public interface IFieldBinder
{
    MemberAssignment Assign(Expression parameter, Type type, GraphQLField field); 
}
