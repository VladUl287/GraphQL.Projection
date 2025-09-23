using GraphQLApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GraphQLApi.Database;

public sealed class DBContext : DbContext
{
    public DBContext()
    {
        //Database.EnsureCreated();
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            //.UseInMemoryDatabase("db");
            .UseNpgsql("Host=localhost;Port=5432;Database=TestDbProjectionV2;Username=postgres;Password=postgres");

        optionsBuilder.LogTo(Console.WriteLine);

        base.OnConfiguring(optionsBuilder);
    }
}
