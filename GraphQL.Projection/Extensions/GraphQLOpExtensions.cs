using GraphQL.Projection.Functors;

namespace GraphQL.Projection.Extensions;

public static class GraphQLOpExtensions
{
    public static GraphQLOp<B> Map<A, B>(this GraphQLOp<A> op, Func<A, B> f)
    {
        return op switch
        {
            GraphQLOp<A>.Field field => new GraphQLOp<B>.Field(
                field.Name,
                fieldNode => f(field.Next(fieldNode))),

            //GraphQLOp<A>.Object obj => new GraphQLOp<B>.Object(
            //    obj.Name,
            //    obj.Selections.Select(s => s.Map(f)).ToList(),
            //    objNode => f(obj.Next(objNode))),

            _ => throw new InvalidOperationException()
        };
    }
}
