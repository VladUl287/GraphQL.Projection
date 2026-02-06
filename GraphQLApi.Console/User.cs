namespace GraphQLApi.Console;

public class User
{
    public int Id { get; init; }
    public string? Name { get; init; }
}

public class ExternalUser : User
{
    public string Metadata { get; init; }
}

public class TemporaryUser : User
{
    public TimeSpan LifeTime { get; init; }
}

public class DeletedExternalUser : ExternalUser
{
    public DateTime DeletedAt { get; init; }
}