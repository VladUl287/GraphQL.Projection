namespace GraphQL.Projection;

public readonly struct TreeField(string name, TreeField[] children)
{
    public required string Name { get; init; } = name ?? string.Empty;

    public TreeField[] Children { get; init; } = children ?? [];
}
