using GraphQLApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GraphQLApi.Database;

public sealed class DBContext : DbContext
{
    public DBContext()
    {
        Database.EnsureCreated();
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Phone> Phones => Set<Phone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var userId = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        modelBuilder.Entity<Tag>().HasData([
            new Tag
            {
                Id = 1,
                Name = "standart"
            },
        ]);

        modelBuilder.Entity<User>().HasData([
            new User
            {
                Id = userId,
                TagId = 1,
            },
            new User
            {
                Id = Guid.NewGuid(),
                TagId = 1
            },
        ]);

        modelBuilder.Entity<Phone>().HasData([
            new Phone
            {
                Id = 1,
                Number = "89964630291",
                UserId = userId2
            },
            new Phone
            {
                Id = 2,
                Number = "89815562830",
                UserId = userId,
            },
            new Phone
            {
                Id = 3,
                Number = "89954650291",
                UserId = userId,
            },
        ]);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseInMemoryDatabase("db");
            //.UseNpgsql("Host=localhost;Port=5432;Database=TestDbProjection;Username=postgres;Password=postgres");

        base.OnConfiguring(optionsBuilder);
    }
}
