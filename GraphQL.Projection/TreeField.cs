namespace GraphQL.Projection;

public readonly struct TreeField(string name, IEnumerable<TreeField> children)
{
    public required string Name { get; init; } = name ?? string.Empty;

    public IEnumerable<TreeField> Children { get; init; } = children ?? Enumerable.Empty<TreeField>();
}
