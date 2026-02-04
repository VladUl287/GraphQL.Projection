using Microsoft.EntityFrameworkCore;

namespace GraphQLApi.Console.Data;

public class AppDatabaseContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=main;Username=postgres;Password=postgres");

        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<User> Users => Set<User>();
}
