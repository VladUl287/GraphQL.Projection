namespace GraphQLApi.Models;

public sealed class User
{
    public Guid Id { get; init; }

    public int TagId { get; init; }
    public Tag Tag { get; init; } = default!;

    public IEnumerable<Phone> Phones { get; init; }
}

public sealed class Tag
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class Phone
{
    public long Id { get; init; }

    public string Number { get; init; } = string.Empty;

    public Guid UserId { get; init; }
    public User User { get; init; }
}