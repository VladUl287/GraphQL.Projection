namespace GraphQL.Projection.Tests.Types;

public sealed class Order
{
    public long Id { get; init; }

    public Address Address { get; init; } = default!;

    public User User { get; init; } = default!;
}
