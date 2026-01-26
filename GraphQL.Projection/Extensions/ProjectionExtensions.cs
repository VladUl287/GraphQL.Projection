using GraphQL.Projection.Functors;
using GraphQL.Projection.Nodes;
using LanguageExt.Common;
using System.Linq.Expressions;

namespace GraphQL.Projection.Extensions;

public static class ProjectionExtensions
{
    public static Result<IQueryable<T>> ConvertToQueryable<T>(this GraphQLOp<ObjectNode> graphQLOp)
    {

        var expressionOp = graphQLOp.Map(objNode =>
        {
            var parameter = Expression.Parameter(typeof(T));

            return Expression.Lambda<Func<T, object>>(null, parameter);
        });

        return default;
    }
}
