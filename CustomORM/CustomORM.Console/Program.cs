using CustomORM.Console.Entities.DV2;
using CustomORM.Console.Entities.Relationals;
using CustomORM.Core;
using CustomORM.Core.Abstractions;
using CustomORM.Core.Extensions;
using Dapper;
using Microsoft.AspNetCore.JsonPatch;
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
        var r1 = new Reclamation 
        { 
            NoReclamation = 0,
            Contenu = "Reclamation de test 01",
            Priorite = "H"
        };
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

        // Create a connection
        using (var connection = new SqlConnection(Config?.GetConnectionString("Default")))
        {
            // Create repository
            var repoClient = new Repository<Client, HClient, int>(connection);
            var repoReclamation = new Repository<Reclamation, HReclamation, int>(connection);

            repoReclamation.Add(ref r1, () => GetNewFunctionnalKey<HReclamation>(connection));

            // Get all
            var clients = repoClient.GetAll();
            var reclamations = repoReclamation.GetAll();

            foreach (var reclamation in reclamations)
                System.Console.WriteLine($"{reclamation.NoReclamation} - {reclamation.Priorite} {reclamation.Contenu}");


            System.Console.WriteLine("-----------BEFORE INSERT-------------------");
            foreach (var client in clients)
                System.Console.WriteLine($"{client.NoClient} - {client.Nom} {client.Prenom} - {client.Adresse1} - {client.DateNaissance?.ToString("yyyy-MM-dd")}");
            System.Console.WriteLine("------------------------------");

            System.Console.WriteLine("Press any key to continue");
            System.Console.ReadKey();

            // Insert            
            repoClient.Add(ref c1, () => GetNewFunctionnalKey<HClient>(connection));      // functional key, it not a job of repository

            System.Console.WriteLine("------------------------------");
            System.Console.WriteLine($"{c1.NoClient} - {c1.Nom} {c1.Prenom} - {c1.Adresse1} - {c1.DateNaissance?.ToString("yyyy-MM-dd")}");
            System.Console.WriteLine($"-----------------------------");

            System.Console.WriteLine("Press any key to continue");
            System.Console.ReadKey();

            // Get all : One more time
            clients = repoClient.GetAll();

            System.Console.WriteLine("-----------AFTER INSERT-------------------");
            foreach (var client in clients)
                System.Console.WriteLine($"{client.NoClient} - {client.Nom} {client.Prenom} - {client.Adresse1} - {client.DateNaissance?.ToString("yyyy-MM-dd")}");
            System.Console.WriteLine("------------------------------");

            // Get one
            var client419 = repoClient.Get(419);
            if (client419 != null)
                System.Console.WriteLine($"{client419.NoClient} - {client419.Nom} {client419.Prenom} - {client419.Adresse1} - {client419.DateNaissance?.ToString("yyyy-MM-dd")}");
            else
                System.Console.WriteLine($"Client 419 not found");

            System.Console.WriteLine("Press any key to continue");
            System.Console.ReadKey();

            // patch 419
            if (client419 != null)
            {
                var patchDoc = new JsonPatchDocument<Client>();
                patchDoc.Replace(e => e.Adresse1, "13");
                patchDoc.Replace(e => e.Adresse2, "Rue abou Atahia");

                repoClient.Patch(client419.NoClient, patchDoc);

                System.Console.WriteLine($"Client {client419.NoClient} patched");
            }

            // Delete last inserted
            repoClient.Delete(c1.NoClient);
            System.Console.WriteLine($"Delete client {c1.NoClient} ok");

            System.Console.WriteLine("Press any key to continue");
            System.Console.ReadKey();

            // Update client283
            var client277 = repoClient.Get(277);
            if (client277 != null)
            {
                System.Console.WriteLine($"{client277.NoClient} - {client277.Nom} {client277.Prenom} - {client277.Adresse1} - {client277.DateNaissance?.ToString("yyyy-MM-dd")}");
                client277.DateNaissance = client277.DateNaissance.Value.AddMonths(1);
                repoClient.Update(ref client277);
                System.Console.WriteLine($"Client {client277.NoClient} updated");
                System.Console.WriteLine($"{client277.NoClient} - {client277.Nom} {client277.Prenom} - {client277.Adresse1} - {client277.DateNaissance?.ToString("yyyy-MM-dd")}");
            }
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
        return sqlConnection.QueryFirst<int>(
            $"SELECT NEXT VALUE FOR {typeof(TEntityDV2).FindTableTargetInformation(TableSqlInfos.Schema)}.seq_{typeof(TEntityDV2).FindTableTargetInformation(TableSqlInfos.Name)}");
    }
}
