namespace GraphQL.Projection.Tests.Types;

public sealed class Order
{
    public long Id { get; init; }

    public string Name { get; init; }

    public Address Address { get; init; } = default!;

    public User User { get; init; } = default!;

    public IEnumerable<Building>? Buildings { get; init; }
}
