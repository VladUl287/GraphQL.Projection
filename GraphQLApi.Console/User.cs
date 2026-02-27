namespace GraphQLApi.Console;

public class BaseUser<T> where T : Role
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public bool Active { get; init; }
    public T Role { get; init; }
}

public class User : BaseUser<Role>
{
    public string Expertise { get; init; }
    public DateTime CreatedAt { get; init; }
    public ICollection<Product> Products { get; init; } = [];
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

public class Product
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public string Name { get; init; }
    public DateTime CreatedAt { get; init; }

    public virtual List<ProductVariant> Variants { get; init; } = [];
}

public sealed class ProductVariant
{
    public Guid Id { get; init; }
    public ProductVariantType Type { get; init; }
    public int Size { get; init; }
}

public enum ProductVariantType
{
    Default,
    Other
}