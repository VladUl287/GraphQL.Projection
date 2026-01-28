namespace GraphQL.Projection;

public interface IGraphQLNode;
public record FieldNode(string Name, ArgumentNode[] Arguments = null) : IGraphQLNode;
public record ObjectNode(string Name, IGraphQLNode[] Selections) : IGraphQLNode;

public interface IGraphQLValue;
public record ArgumentNode(string Name, IGraphQLValue Value);

public abstract record GraphQLOp<A>
{
    public sealed record Field(string Name, ArgumentNode[] Arguments, Func<FieldNode, A> Next) : GraphQLOp<A>;
    public sealed record Object(string Name, GraphQLOp<A>[] Selections, Func<ObjectNode, A> Next) : GraphQLOp<A>;
}
