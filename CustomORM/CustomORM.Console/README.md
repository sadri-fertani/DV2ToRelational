# Introduction

# Transformation
Il faut Transformer les entites (seulement les Hub) par EF vers des Dto
Pour ce besoin, il faut utiliser CustomORM.Converter

```
using Dapper;
using Dapper.FluentMap;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PocDapper.Dtos;
using PocDapper.Dtos2;
using PocDapper.Entites.DV;
using PocDapper.Mapper;
using Serilog;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Transactions;

namespace PocDapper
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            #region Load config
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();

            // Configure FluentMapper for Dapper
            FluentMapper.Initialize(config =>
            {
                config.AddMap(new HubMap1());
                config.AddMap(new HubMap2());
                config.AddMap(new HubMap3());
            });

            // Logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(@"Logs\log.txt", buffered: true)
            .CreateLogger();
            #endregion

            #region Load data
            // Charger les 677 clients
            var clientsDto = File.ReadFile<List<ClientDto>>(@"Data\Clients.json") ?? [];

            // Charger les 292 reclamations
            var reclamationsDto = File.ReadFile<List<ReclamationDto>>(@"Data\Reclamation.json") ?? [];
            #endregion

            //TestInsertHubClient(config, clientsDto);

            //TestInsertHubReclamation(config, reclamationsDto);

            //TestSchema(config);

            SimulerInsertionDatabaseRelationnelle(config);

            //TestLecture(config);

            // Close and flush logger
            Log.CloseAndFlush();
        }

        /// <summary>
        /// Calculer le temps d'exec pour l'insertion de 677 clients
        /// </summary>
        /// <param name="config"></param>
        /// <param name="clientsDto"></param>
        static void TestInsertHubClient(IConfiguration config, List<ClientDto> clientsDto)
        {
            var sw = new Stopwatch();

            Console.WriteLine("Begin - TestInsertHubClient");
            sw.Start();

            // Create a connection
            using (var connection = new SqlConnection(config.GetConnectionString("Default")))
            {
                foreach (var cl in clientsDto)
                {
                    // One transaction per one client
                    using (var transactionScope = new TransactionScope())
                    {
                        try
                        {
                            // Date ajout, User & Source
                            var LoadDts = DateTime.Now;
                            var LoadUser = Environment.UserName;
                            var LoadSrc = AppDomain.CurrentDomain.FriendlyName;

                            // Get new no_client
                            string sqlGetNewNoClient = "SELECT NEXT VALUE FOR dbo.seq_h_client";
                            var noClient = connection.QueryFirst<int>(sqlGetNewNoClient);
                            // compute sha256
                            var clientHk = noClient.ToString().ToSha256();

                            Console.WriteLine($"No client: {noClient}");

                            // Insertion into hub h_client
                            string requestInsertHub = @"
		                    INSERT INTO [dbo].[h_client]
			                     VALUES
			                     (
				                     @ClientHK
				                    ,@LoadDts
				                    ,@LoadUser
				                    ,@LoadSrc
				                    ,@NoClient
			                     )";

                            var rowsAffected = connection.Execute(
                                requestInsertHub,
                                new
                                {
                                    ClientHK = clientHk,
                                    LoadDts,
                                    LoadUser,
                                    LoadSrc,
                                    NoClient = noClient
                                });

                            if (!rowsAffected.Equals(0))
                            {
                                Console.WriteLine($"New Client '{noClient}/{clientHk}' inserted.");

                                // Insert into sattelite s_client_adresse
                                string requestInsertHubAdresse = @"
                                INSERT INTO [dbo].[s_client_adresse]
			                                 VALUES
			                                (
					                                @ClientHK
				                                   ,@LoadDts
				                                   ,@LoadUser
				                                   ,@LoadSrc
				                                   ,@Adresse1
				                                   ,@Adresse2
				                                   ,@Adresse3
				                                   ,@Cite
				                                   ,@Provence
				                                   ,@Pays
				                                   ,@CodePostale
			                                )";

                                rowsAffected = connection.Execute(
                                requestInsertHubAdresse,
                                new
                                {
                                    ClientHK = clientHk,
                                    LoadDts,
                                    LoadUser,
                                    LoadSrc,
                                    cl.Adresse1,
                                    cl.Adresse2,
                                    cl.Adresse3,
                                    cl.Cite,
                                    cl.Provence,
                                    cl.Pays,
                                    cl.CodePostale
                                });

                                if (!rowsAffected.Equals(0))
                                {
                                    Console.WriteLine($"Sattelite 1 Adresse for Client '{noClient}/{clientHk}' inserted.");

                                    // Insert into sattelite s_client_identification
                                    string requestInsertHubIdentification = @"
                                        INSERT INTO [dbo].[s_client_identification]
			                             VALUES
			                            (
					                            @ClientHK
				                                ,@LoadDts
				                                ,@LoadUser
				                                ,@LoadSrc
				                               ,@Prenom
				                               ,@Nom
				                               ,@Sexe
				                               ,@DateNaissance
				                               ,@DateDeces
				                               ,@Langue
			                            )";

                                    rowsAffected = connection.Execute(
                                    requestInsertHubIdentification,
                                    new
                                    {
                                        ClientHK = clientHk,
                                        LoadDts,
                                        LoadUser,
                                        LoadSrc,
                                        cl.Prenom,
                                        cl.Nom,
                                        cl.Sexe,
                                        cl.DateNaissance,
                                        cl.DateDeces,
                                        cl.Langue
                                    });

                                    if (!rowsAffected.Equals(0))
                                    {
                                        Console.WriteLine($"Sattelite 2 Identification for Client '{noClient}/{clientHk}' inserted.");

                                        // Insert into pit p_client
                                        string requestInsertPit = @"
                                        INSERT INTO [dbo].[p_client]
			                                VALUES
			                                (
				                                @ClientHK,
				                                @LoadDts,
				                                NULL,
				                                @LoadUser,
				                                @LoadSrc,
				                                @LoadDts,
				                                @LoadDts
			                                )";

                                        rowsAffected = connection.Execute(
                                        requestInsertPit,
                                        new
                                        {
                                            ClientHK = clientHk,
                                            LoadDts,
                                            LoadUser,
                                            LoadSrc
                                        });

                                        if (!rowsAffected.Equals(0))
                                        {
                                            Console.WriteLine($"PIT for Client '{noClient}/{clientHk}' inserted.");

                                            // ALL OK => commit transaction
                                            transactionScope.Complete();
                                        }
                                        else
                                        {
                                            Log.Error($"PIT for Client '{noClient}/{clientHk}' not inserted.");
                                            Console.WriteLine($"PIT for Client '{noClient}/{clientHk}' not inserted.");
                                        }
                                    }
                                    else
                                    {
                                        Log.Error($"Sattelite 2 Identification for Client '{noClient}/{clientHk}' not inserted.");
                                        Console.WriteLine($"Sattelite 2 Identification for Client '{noClient}/{clientHk}' not inserted.");
                                    }
                                }
                                else
                                {
                                    Log.Error($"Sattelite 1 Adresse for Client '{noClient}/{clientHk}' not inserted.");
                                    Console.WriteLine($"Sattelite 1 Adresse for Client '{noClient}/{clientHk}' not inserted.");
                                }
                            }
                            else
                            {
                                Log.Error($"Client '{noClient}/{clientHk}' not inserted.");
                                Console.WriteLine($"Client '{noClient}/{clientHk}' not inserted.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Fatal(ex.Message);
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }

            sw.Stop();
            Console.WriteLine("End - TestInsertHubClient");

            Console.WriteLine($"Clients : {clientsDto.Count}");
            Log.Information($"Clients : {clientsDto.Count}");

            Console.WriteLine($"Total ellapsed : {sw.ElapsedMilliseconds} ms");
            Log.Information($"Total ellapsed : {sw.ElapsedMilliseconds} ms");

            Console.WriteLine($"Average ellapsed : {sw.ElapsedMilliseconds / clientsDto.Count} ms");
            Log.Information($"Average ellapsed : {sw.ElapsedMilliseconds / clientsDto.Count} ms");
        }

        /// <summary>
        /// Calculer le temps d'exec pour l'insertion de 292 reclmations
        /// </summary>
        /// <param name="config">config</param>
        /// <param name="reclamationsDto">reclamationsDto</param>
        static void TestInsertHubReclamation(IConfiguration config, List<ReclamationDto> reclamationsDto)
        {

        }

        /// <summary>
        /// Calculer le temps d'exec pour la recuperation des clients avec les satellites
        /// </summary>
        /// <param name="config">config</param>
        static void TestLecture(IConfiguration config)
        {
            List<Client2Dto> clients2;

            // Create a connection
            using (var connection = new SqlConnection(config.GetConnectionString("Default")))
            {
                connection.Open(SqlConnectionOverrides.OpenWithoutRetry);

                clients2 = connection
                    .Query<Client2Dto, ClientInfo2Dto, ClientAdresse2Dto, Client2Dto>
                    (
                        @"
                        SELECT 
                            [no_client], 
                            [prenom], [nom], [sexe], [date_naissance], [date_deces], [langue], 
                            [adresse1], [adresse2], [adresse3], [cite], [provence], [pays], [code_postale] 
                        FROM 
                            [dbo].[v_client]",
                        (cl, inf, adr) =>
                        {
                            cl.ClientInfos = inf;
                            cl.ClientAdresse = adr;

                            return cl;
                        },
                        splitOn: "no_client, prenom, adresse1"
                    )
                    .ToList();

                connection.Close();
            }
        }

        static string GetNamesColumns(this Type className)
        {
            var namespaceOfEntite = className.Namespace;
            List<string> columnsNames = new List<string>();

            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(className))
            {
                if (!prop.PropertyType.FullName!.Contains(namespaceOfEntite!))
                {
                    columnsNames.Add($"@{prop.Name}");
                }
            }

            return string.Join(",", columnsNames);
        }

        static void SimulerInsertionDatabaseRelationnelle(IConfiguration config)
        {
            // 0- input dto
            var dto = new ClientDto
            {
                Adresse1 = "ad.2",
                Adresse2 = "ad.3",
                Adresse3 = "ad.4",
                Cite = "TUNIS",
                Provence = "AR",
                Pays = "TN",
                CodePostale = "2083",
                Nom = "Gasmi",
                Prenom = "Hanen",
                Sexe = "F",
                DateDeces = null,
                DateNaissance = DateTime.Parse("1981-01-19"),
                Langue = "AR"
            };

            // 1- Hub name : h_client => HClient
            var hubName = "h_client";

            // Create a connection
            using (var connection = new SqlConnection(config.GetConnectionString("Default")))
            {
                // Date ajout, User & Source
                var LoadDts = DateTime.Now;
                var LoadUser = Environment.UserName;
                var LoadSrc = AppDomain.CurrentDomain.FriendlyName;

                // Get new identifier from a sequence
                string sqlGetNewNo = $"SELECT NEXT VALUE FOR dbo.seq_{hubName}";
                var no = connection.QueryFirst<int>(sqlGetNewNo);
                // compute sha256
                var hk = no.ToString().ToSha256();

                var hc = new HClient
                {
                    HClientHk = hk,
                    HLoadDts = LoadDts,
                    HLoadUser = LoadUser,
                    HLoadSrc = LoadSrc,
                    NoClient = no
                };

                // 2- insert into hub : ok nothing special
                string requestInsertHub = @$"INSERT INTO [dbo].[{hubName}] VALUES ({typeof(HClient).GetNamesColumns()})";

                var rowsAffected = connection.Execute(
                    requestInsertHub,
                    hc);


                // 3- get info hub
                var namespaceOfEntite = typeof(HClient).Namespace;

                foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(typeof(HClient)))
                {
                    if (prop.PropertyType.FullName!.Contains(namespaceOfEntite!))
                    {
                        // Collection or object
                        if (prop.PropertyType.Name == "ICollection`1")
                        {
                            Console.WriteLine($"Collection of : {prop.PropertyType.GenericTypeArguments.First().FullName}");

                            // Insertion dans la table cible

                            // exemple : 		FullName : "PocDapper.Entites.DV.SClientIdentification"
                            var sattelite = Activator.CreateInstance(Type.GetType(prop.PropertyType.GenericTypeArguments.First().FullName!)!);

                            // Load from Dto
                            ChargerSatellite(sattelite!, hc, dto, namespaceOfEntite);

                            // load from Hub
                            SetObjectProperty("HClientHk", hc.HClientHk, sattelite!);

                            // complete as Sat
                            (sattelite as ISattelite)!.SLoadDts = LoadDts;
                            (sattelite as ISattelite)!.SLoadUser = LoadUser;
                            (sattelite as ISattelite)!.SLoadSrc = LoadSrc;

                            // Insert into db
                            string requestInsertSat = string.Empty;
                            switch (prop.PropertyType.GenericTypeArguments.First().FullName)
                            {
                                case "PocDapper.Entites.DV.SClientIdentification":
                                    requestInsertSat = @$"INSERT INTO [dbo].[s_client_identification] VALUES ({Type.GetType(prop.PropertyType.GenericTypeArguments.First().FullName).GetNamesColumns()})";
                                    break;
                                case "PocDapper.Entites.DV.SClientAdresse":
                                    requestInsertSat = @$"INSERT INTO [dbo].[s_client_adresse] VALUES ({Type.GetType(prop.PropertyType.GenericTypeArguments.First().FullName).GetNamesColumns()})";
                                    break;
                            }

                            var rowsSatAffected = connection.Execute(
                                requestInsertSat,
                                sattelite);
                        }
                        else
                        {
                            Console.WriteLine($"Object of : {prop.PropertyType.FullName}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Field : {prop.Name}");
                    }
                }


                // insert into pit
                string requestInsertPit = @"
                                        INSERT INTO [dbo].[p_client]
			                                VALUES
			                                (
				                                @ClientHK,
				                                @LoadDts,
				                                NULL,
				                                @LoadUser,
				                                @LoadSrc,
				                                @LoadDts,
				                                @LoadDts
			                                )";

                rowsAffected = connection.Execute(
                requestInsertPit,
                new
                {
                    ClientHK = hc.HClientHk,
                    LoadDts,
                    LoadUser,
                    LoadSrc
                });
            }
        }

        private static void SetObjectProperty(string propertyName, object value, object obj)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName)!;
            
            if (propertyInfo != null)
                propertyInfo.SetValue(obj, value, null);
        }

        private static object? GetObjectProperty(string propertyName, object obj)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName)!;
            
            return propertyInfo != null ? 
                propertyInfo.GetValue(obj, null) :
                null;
        }

        static void ChargerSatellite(object sattelite, object hc, object dto, string namespaceOfEntite)
        {
            dynamic dynSat = sattelite;
            dynamic dynDto = dto;

            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(sattelite))
            {
                if (!prop.PropertyType.FullName!.Contains(namespaceOfEntite))
                {
                        SetObjectProperty(
                            prop.Name,
                            GetObjectProperty(prop.Name, dynDto), 
                            sattelite);
                }
            }
        }

        /// <summary>
        /// Get infos database
        /// </summary>
        /// <param name="config">config</param>
        static void TestSchema(IConfiguration config)
        {
            var loadDts = DateTime.Now;
            var loadSrc = "PocDapper";
            var loadUser = "SFE093";

            var c = new HClient
            {
                HClientHk = "",
                HLoadDts = loadDts,
                HLoadSrc = loadSrc,
                HLoadUser = loadUser,
                NoClient = 10001
            };


            var a = new SClientAdresse
            {
                SLoadDts = loadDts,
                SLoadSrc = loadSrc,
                SLoadUser = loadUser,

                Adresse1 = "a1",
                Adresse2 = "a2",
                Adresse3 = "a3",
                Cite = "",
                CodePostale = "456",
                Pays = "Tunisie",
                Provence = "QC",

                HClientHk = c.HClientHk,
                HClientHkNavigation = c
            };

            var i = new SClientIdentification
            {
                SLoadDts = loadDts,
                SLoadSrc = loadSrc,
                SLoadUser = loadUser,

                DateNaissance = DateTime.Now,
                DateDeces = DateTime.Now,
                Langue = "FR",
                Nom = "Fertani",
                Prenom = "Sadri",
                Sexe = "M",

                HClientHk = c.HClientHk,
                HClientHkNavigation = c
            };

            c.SClientAdresses = new List<SClientAdresse>() { a };
            c.SClientIdentifications = new List<SClientIdentification>() { i };

            // Create a connection
            using (var connection = new SqlConnection(config.GetConnectionString("Default")))
            {
                connection.Open(SqlConnectionOverrides.OpenWithoutRetry);

                DataTable allTablesSchemaTable = connection.GetSchema("Tables");

                ShowDataTable(allTablesSchemaTable);

                Console.WriteLine(Environment.NewLine);

                // First, get schema information of all the columns in current database.
                DataTable allColumnsSchemaTable = connection.GetSchema("Columns");

                ShowColumns(allColumnsSchemaTable);

                connection.Close();
            }
        }

        private static void ShowColumns(DataTable columnsTable)
        {
            var selectedRows = columnsTable
                .AsEnumerable()
                .Select
                (
                    info => new
                    {
                        //TableCatalog = info["TABLE_CATALOG"],
                        TableSchema = info["TABLE_SCHEMA"],
                        TableName = info["TABLE_NAME"],
                        ColumnName = info["COLUMN_NAME"],
                        DataType = info["DATA_TYPE"]
                    }
                );

            Console.WriteLine("{0,-15}{1,-35}{2,-20}{3,-20}", "TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME", "DATA_TYPE");

            foreach (var row in selectedRows)
            {
                Console.WriteLine(
                    "{0,-15}{1,-35}{2,-20}{3,-20}",
                    //row.TableCatalog,
                    row.TableSchema,
                    row.TableName,
                    row.ColumnName,
                    row.DataType);
            }
        }

        private static void ShowDataTable(DataTable table)
        {
            var rows = table.Rows.Cast<DataRow>().Where(dr => dr.ItemArray[3]!.ToString() == "BASE TABLE" && dr.ItemArray[2]!.ToString() != "sysdiagrams");

            var columns = table.Columns.Cast<DataColumn>().Where(dc => dc.ColumnName == "TABLE_NAME");

            foreach (var currentRow in rows)
            {
                Console.WriteLine(currentRow.ItemArray[2]!.ToString());
            }


            //foreach (DataRow row in table.Rows)
            //{
            //    foreach (DataColumn col in table.Columns)
            //    {
            //        if (col.DataType.Equals(typeof(DateTime)))
            //            Console.Write("{0,-" + tabulation + ":d}", row[col]);
            //        else if (col.DataType.Equals(typeof(Decimal)))
            //            Console.Write("{0,-" + tabulation + ":C}", row[col]);
            //        else
            //            Console.Write("{0,-" + tabulation + "}", row[col]);
            //    }
            //    Console.WriteLine();
            //}
        }
    }
}

/*
 * 
 * var rqSelect = "SELECT * FROM [dbo].[h_client]";
 * 
 * var clients = connection.Query<HubClient>(rqSelect);
 * 
 */
```