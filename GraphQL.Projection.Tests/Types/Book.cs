namespace GraphQL.Projection.Tests.Types;

public sealed class Book
{
    public long Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public ICollection<Genre> Genres { get; init; } = default!;
}
