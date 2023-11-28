namespace GraphQL.Projection;

public readonly struct EntityField(string name, IEnumerable<EntityField> subFields)
{
    public required string Name { get; init; } = name;

    public IEnumerable<EntityField> SubFields { get; init; } = subFields;
}
