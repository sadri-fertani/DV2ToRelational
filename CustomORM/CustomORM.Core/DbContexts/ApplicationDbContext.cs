using Microsoft.EntityFrameworkCore;

namespace CustomORM.Core.DbContexts;

public class ApplicationDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseLazyLoadingProxies()
            .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDb;Initial Catalog=RemoveMeDB;Integrated Security=True");
    }

    public DbSet<T> GetDbSet<T>() where T : class
    {
        return this.Set<T>();
    }
}
