using CustomORM.Console.Entities.DV2;
using CustomORM.Console.Entities.Relationals;
using CustomORM.Core;
using CustomORM.Core.Extensions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CustomORM.Console;

public static class Program
{
    private static IConfiguration? Config { get; set; }

    static void Init()
    {
        #region Load appsettings.json
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        Config = builder.Build();
        #endregion

        #region Config logger
        // Logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"Logs\log.txt", buffered: true)
        .CreateLogger();
        #endregion
    }

    public static void Main()
    {
        // Init environment
        Init();

        // Create object
        var c1 = new Client
        {
            NoClient = 0,
            Adresse1 = "8420",
            Adresse2 = "Rue de Bergen",
            Adresse3 = "",
            Cite = "QUEBEC",
            Provence = "QC",
            Pays = "CA",
            CodePostale = "G2C 2H8",
            Nom = "Fertani",
            Prenom = "Sadri",
            Sexe = "M",
            DateDeces = null,
            DateNaissance = DateTime.Parse("1980-03-28"),
            Langue = "AR"
        };

        // Update object
        var c2 = new Client
        {
            NoClient = 283,
            Adresse1 = "8420",
            Adresse2 = "Rue de Bergen",
            Adresse3 = "",
            Cite = "QUEBEC",
            Provence = "QC",
            Pays = "CA",
            CodePostale = "G2C 2H8",
            Nom = "Fertani",
            Prenom = "Sadri",
            Sexe = "M",
            DateDeces = null,
            DateNaissance = DateTime.Now,
            Langue = "AR"
        };

        // Create a connection
        using (var connection = new SqlConnection(Config!.GetConnectionString("Default")))
        {
            // Create repository
            var repo = new Repository<Client, HClient, int>(connection);

            // Get all
            var clients = repo.GetAll();

            System.Console.WriteLine("-----------BEFORE INSERT-------------------");
            foreach (var client in clients)
            {
                System.Console.WriteLine($"{client.NoClient} - {client.Nom} {client.Prenom} - {client.Adresse1}");
            }
            System.Console.WriteLine("------------------------------");

            System.Console.WriteLine("Press any key to continue");
            System.Console.ReadKey();

            // Insert            
            repo.Add(ref c1, () => GetNewFunctionnalKey<HClient>(connection));      // functional key, it not a job of repository

            System.Console.WriteLine("------------------------------");
            System.Console.WriteLine($"{c1.NoClient} - {c1.Nom} {c1.Prenom}");
            System.Console.WriteLine($"-----------------------------");

            System.Console.WriteLine("Press any key to continue");
            System.Console.ReadKey();

            // Get all : One more time
            clients = repo.GetAll();

            System.Console.WriteLine("-----------AFTER INSERT-------------------");
            foreach (var client in clients)
            {
                System.Console.WriteLine($"{client.NoClient} - {client.Nom} {client.Prenom} - {client.Adresse1}");
            }
            System.Console.WriteLine("------------------------------");

            // Get one
            var client283 = repo.Get(283);
            System.Console.WriteLine($"{client283.NoClient} - {client283.Nom} {client283.Prenom} - {client283.Adresse1}");

            System.Console.WriteLine("Press any key to continue");
            System.Console.ReadKey();

            // Delete last inserted
            repo.Delete(c1.NoClient);
            System.Console.WriteLine($"Delete client {c1.NoClient} ok");

            System.Console.WriteLine("Press any key to continue");
            System.Console.ReadKey();

            // Update c2
            repo.Update(ref c2);
            System.Console.WriteLine($"Client {c2.NoClient} updated");

        }

        // Close and flush logger
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Get new id from sequence
    /// </summary>
    /// <param name="sqlConnection"></param>
    /// <returns></returns>
    private static int GetNewFunctionnalKey<TEntityDV2>(SqlConnection sqlConnection)
    {
        return sqlConnection.QueryFirst<int>($"SELECT NEXT VALUE FOR {typeof(TEntityDV2).FindSchemaTableTarget()}.seq_{typeof(TEntityDV2).FindTableTarget()}");
    }
}
