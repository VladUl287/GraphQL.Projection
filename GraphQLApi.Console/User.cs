namespace GraphQLApi.Console;

public sealed class User
{
    public int Id { get; init; }
    public string? Name { get; init; }
}

public sealed class ExternalUser
{
    public string Metadata { get; init; }
}

public sealed class TemporaryUser
{
    public TimeSpan LifeTime { get; init; }
}
