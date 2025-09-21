using System.Linq.Expressions;

namespace GraphQL.Projection.Resolvers.Contracts;

public interface IParameterResolver
{
    ParameterExpression GetParameterExpression(MemberInitExpression memberInit);
}
