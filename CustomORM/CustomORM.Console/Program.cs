using CustomORM.Console.Entities.DV2;
using CustomORM.Console.Entities.Relationals;
using CustomORM.Core;
using CustomORM.Core.Extensions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog;
using static Dapper.SqlMapper;

namespace CustomORM.Console;

public class Program
{
    private static IConfiguration? Config { get; set; }

    static void Init()
    {
        #region Load config
        var builder = new ConfigurationBuilder();
        builder.SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        Config = builder.Build();

        // Logger
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(@"Logs\log.txt", buffered: true)
        .CreateLogger();
        #endregion
    }

    public static void Main(string[] args)
    {
        // Init environment
        Init();

        // Create object
        var c = new Client
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

        // Create a connection
        using (var connection = new SqlConnection(Config!.GetConnectionString("Default")))
        {
            // Create repository
            var repo = new Repository<Client, HClient>(connection);
            
            // get all
            var clients = repo.GetAll();

            System.Console.WriteLine("------------------------------");
            foreach (var client in clients)
            {
                System.Console.WriteLine($"{client.NoClient} - {client.Nom} {client.Prenom} - {client.Adresse1}");
            }
            System.Console.WriteLine("------------------------------");

            // insert
            // functional key, it not a job of repository
            repo.Insert(ref c, GetNewFunctionnalKey<HClient>(connection));

            System.Console.WriteLine("------------------------------");
            System.Console.WriteLine($"{c.NoClient} - {c.Nom} {c.Prenom}");
            System.Console.WriteLine($"-----------------------------");
        }
        // Close and flush logger
        Log.CloseAndFlush();
    }

    /// <summary>
    /// Get new id from sequence
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    private static int GetNewFunctionnalKey<TEntityRelationnal>(SqlConnection sqlConnection)
    {
        return sqlConnection.QueryFirst<int>($"SELECT NEXT VALUE FOR {typeof(TEntityRelationnal).FindSchemaTableTarget()}.seq_{typeof(TEntityRelationnal).FindTableTarget()}");
    }
}
