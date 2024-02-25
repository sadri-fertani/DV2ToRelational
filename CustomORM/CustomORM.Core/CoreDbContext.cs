//using Microsoft.EntityFrameworkCore;
//using System.Collections.Generic;

//namespace CustomORM.Core;

//public class CoreDbContext : DbContext
//{
//    public DbSet<Student> Students { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//    {
//        //optionsBuilder
//        //    .UseLazyLoadingProxies()
//        //    .UseInMemoryDatabase(databaseName: "InMemoryLogsDb");

//        optionsBuilder
//            .UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDb;Initial Catalog=RemoveMeDB;Integrated Security=True");
//    }

//    public DbSet<T> GetDbSet<T>() where T : class
//    {
//        return this.Set<T>();
//    }
//}

//using Microsoft.EntityFrameworkCore;

//var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
//optionsBuilder.UseSqlServer(Config!.GetConnectionString("Default"));

//using (var dbContext = new DV2DbContext(optionsBuilder.Options, 25))
//{
//    var clients = dbContext.VClients.OrderBy(c => c.NoClient).Take(25).ToList();

//    foreach (var item in clients)
//    {
//        Console.WriteLine($"{item.NoClient} - {item.Nom} {item.Prenom}");
//    }
//}