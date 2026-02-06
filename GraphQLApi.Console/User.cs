namespace GraphQLApi.Console;

public class BaseUser<T> where T : Role
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public T Role { get; init; }
}

public class User : BaseUser<Role>
{
    public DateTime CreatedAt { get; init; }
}

public class ExternalUser : BaseUser<ExternalRole>
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

public class Role
{
    public int Id { get; init; }
    public string Name { get; init; }
}

public class ExternalRole : Role
{
    public string Source { get; init; }
}