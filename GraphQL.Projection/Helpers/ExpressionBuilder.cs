using GraphQL.Projection.Resolvers.Contracts;
using GraphQLParser.AST;
using System.Linq.Expressions;

namespace GraphQL.Projection.Helpers;

public sealed class ExpressionBuilder(ITypeBuilder typeBuilder, IParameterResolver parameterResolver)
{
    private readonly ITypeBuilder typeBuilder = typeBuilder;
    private readonly IParameterResolver parameterResolver = parameterResolver;

    public Expression<Func<TEntity, TEntity>> BuildExpression<TEntity>(GraphQLSelectionSet node)
    {
        var memberInit = typeBuilder.BuildType(typeof(TEntity), node);

        var parameter = parameterResolver.GetParameterExpression(memberInit);

        return Expression.Lambda<Func<TEntity, TEntity>>(memberInit, parameter);
    }
}