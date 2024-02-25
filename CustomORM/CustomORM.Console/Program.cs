using CustomORM.Console.Entities;
using CustomORM.Core;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog;

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
            var repo = new Repository<Client>(connection);
            repo.Insert(ref c);
        }

        System.Console.WriteLine("------------------------------");
        System.Console.WriteLine($"{c.NoClient} - {c.Nom} {c.Prenom}");
        System.Console.WriteLine($"-----------------------------");

        // Close and flush logger
        Log.CloseAndFlush();
    }
}
