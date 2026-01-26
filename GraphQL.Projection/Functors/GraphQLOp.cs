using GraphQL.Projection.Nodes;

namespace GraphQL.Projection.Functors;

public abstract record GraphQLOp<A>
{
    public record Field(string Name, Func<FieldNode, A> Next) : GraphQLOp<A>;
    public record Object(string Name, List<A> Selections, Func<ObjectNode, A> Next) : GraphQLOp<A>;
}