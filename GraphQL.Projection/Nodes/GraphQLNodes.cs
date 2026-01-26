namespace GraphQL.Projection.Nodes;

public record FieldNode(string Name);
public record ObjectNode(string Name);
public record ArgumentNode(string Name, object Value);
