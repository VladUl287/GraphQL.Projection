namespace GraphQLApi.Models;

public sealed class User
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Phone { get; init; }
    public string Index { get; init; }
}