using CustomORM.Abstractions;
using CustomORM.Core.Extensions;
using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;
using System.ComponentModel;
using System.Reflection;
using System.Transactions;

namespace CustomORM.Core
{
    public sealed class Repository<TEntityRelationnal, THub> : IRepository<TEntityRelationnal, THub>
        where TEntityRelationnal : class, new()
        where THub : class, new()
    {
        private SqlConnection _sqlConnection { get; set; }

        public Repository(SqlConnection sqlConnection)
        {
            this._sqlConnection = sqlConnection;
            DefaultTypeMap.MatchNamesWithUnderscores = true;
        }

        /// <summary>
        ///  Create a new DV entry
        /// </summary>
        /// <param name="entity">The entity to be inserted</param>
        /// <param name="functionnalKey"></param>
        /// <exception cref="Exception"></exception>
        public void Insert(ref TEntityRelationnal entity, int functionnalKey)
        {
            using (var transactionScope = new TransactionScope())
            {
                try
                {
                    // Audit infos
                    var LoadDts = DateTime.Now;
                    var LoadUser = Environment.UserName;
                    var LoadSrc = AppDomain.CurrentDomain.FriendlyName;

                    // New functionnal id => hash256
                    var hashId = functionnalKey.ToSha256();

                    // 1- New instance of hub
                    var hub = Activator.CreateInstance(typeof(THub))! as THub;

                    // 2- Insert key and hash
                    SpyIL.SetAuditInfo<THub>(ref hub!, hub!.FindKey<THub>(), hashId);
                    SpyIL.SetAuditInfo<THub>(ref hub, entity.FindKey<TEntityRelationnal>(), functionnalKey);// Construction : TEntityRelationnal has one key : It's the same functionnal key in THub

                    // Inject audit infos to hub
                    SpyIL.SetAuditInfo<THub>(ref hub, nameof(IHub.HLoadDts), LoadDts);
                    SpyIL.SetAuditInfo<THub>(ref hub, nameof(IHub.HLoadUser), LoadUser);
                    SpyIL.SetAuditInfo<THub>(ref hub, nameof(IHub.HLoadSrc), LoadSrc);

                    // 3- insert into hub        
                    var rowsHubAffected = _sqlConnection.Execute(
                        @$"INSERT INTO [{typeof(THub).FindSchemaTableTarget()}].[{typeof(THub).FindTableTarget()}] VALUES ({typeof(THub)!.GetNamesColumns()})",
                        hub.ConvertToParamsRequest(),
                        commandType: System.Data.CommandType.Text);

                    if (rowsHubAffected > 0)
                        Log.Information("Insertion HUB : OK");
                    else
                    {
                        Log.Fatal("ERROR INSERT HUB");
                        throw new Exception("ERROR INSERT HUB");
                    }

                    // 4- insert into Satellites            
                    #region Satellites
                    // Foreign key between Satellite and Pit
                    Dictionary<string, DateTime> SatelliteLdtsForeignKeys = new Dictionary<string, DateTime>();
                    foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(typeof(THub)!))
                    {
                        if (
                            prop.PropertyType.FullName!.Contains(typeof(THub)!.Namespace!) &&
                            (prop.PropertyType.Name == "ICollection`1") &&
                            prop.Name.StartsWith("S"))
                        {
                            // 1- new instance of Satellite
                            var satellite = SpyIL.GetInstance(prop.PropertyType.GenericTypeArguments.First().FullName!);

                            // 2- Insert key and hash
                            SpyIL.SetAuditInfo<object>(ref satellite, hub.FindKey<THub>(), hashId);

                            // 3.1- Inject audit infos to satellite
                            SpyIL.SetAuditInfo<object>(ref satellite, nameof(ISatellite.SLoadDts), LoadDts);
                            SpyIL.SetAuditInfo<object>(ref satellite, nameof(ISatellite.SLoadUser), LoadUser);
                            SpyIL.SetAuditInfo<object>(ref satellite, nameof(ISatellite.SLoadSrc), LoadSrc);

                            // 3.2- Save foreign key
                            SatelliteLdtsForeignKeys.Add(
                                ExtractShortNameSatellite
                                (
                                    prop.PropertyType.GenericTypeArguments.First().Name,
                                    typeof(TEntityRelationnal).Name
                                ),
                                LoadDts);

                            // 4- Load from Dto
                            SpyIL.ChargerSatellite(ref satellite!, hub, entity, typeof(THub).Namespace!);

                            // 5- Insert into db
                            Type typeSatellite = Assembly.GetEntryAssembly()!.GetTypes().Where(t => t.FullName == prop.PropertyType.GenericTypeArguments.First().FullName).First();

                            var rowsSatelliteAffected = _sqlConnection.Execute(
                                @$"INSERT INTO [{typeSatellite.FindSchemaTableTarget()}].[{typeSatellite.FindTableTarget()}] VALUES ({typeSatellite.GetNamesColumns()})",
                                satellite.ConvertToParamsRequest(typeSatellite),
                                commandType: System.Data.CommandType.Text);

                            if (rowsSatelliteAffected > 0)
                                Log.Information("Insertion Satellite : OK");
                            else
                            {
                                Log.Fatal("ERROR INSERT SATELLITE");
                                throw new Exception("ERROR INSERT SATELLITE");
                            }
                        }
                    }
                    #endregion

                    // 4- insert into PIT
                    #region Point in time
                    foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(typeof(THub)!))
                    {
                        if (
                            prop.PropertyType.FullName!.Contains(typeof(THub)!.Namespace!) &&
                            (prop.PropertyType.Name == "ICollection`1") &&
                            prop.Name.StartsWith("P"))
                        {
                            // 1- new instance of PIT
                            var pit = SpyIL.GetInstance(prop.PropertyType.GenericTypeArguments.First().FullName!);

                            // 2- Insert key and hash
                            SpyIL.SetAuditInfo<object>(ref pit, hub.FindKey<THub>(), hashId);

                            // 3- Inject audit infos to pit
                            SpyIL.SetAuditInfo<object>(ref pit, nameof(IPit.PLoadDts), LoadDts);
                            SpyIL.SetAuditInfo<object>(ref pit, nameof(IPit.PLoadEndDts), null);
                            SpyIL.SetAuditInfo<object>(ref pit, nameof(IPit.PLoadUser), LoadUser);
                            SpyIL.SetAuditInfo<object>(ref pit, nameof(IPit.PLoadSrc), LoadSrc);

                            // 4- Inject foreigns keys
                            foreach (var kv in SatelliteLdtsForeignKeys)
                                SpyIL.SetAuditInfo<object>(ref pit, kv.Key, kv.Value);

                            // 5- Insert into db
                            Type typePit = Assembly.GetEntryAssembly()!.GetTypes().Where(t => t.FullName == prop.PropertyType.GenericTypeArguments.First().FullName).First();

                            var rowsPitAffected = _sqlConnection.Execute(
                                @$"INSERT INTO [{typePit.FindSchemaTableTarget()}].[{typePit.FindTableTarget()}] VALUES ({typePit.GetNamesColumns()})",
                                pit.ConvertToParamsRequest(typePit),
                                commandType: System.Data.CommandType.Text);

                            if (rowsPitAffected > 0)
                                Log.Information("Insertion Pit : OK");
                            else
                            {
                                Log.Fatal("ERROR INSERT PIT");
                                throw new Exception("ERROR INSERT PIT");
                            }
                        }
                    }
                    #endregion

                    // Inject functional key into entity
                    SpyIL.SetFunctionnalKey<TEntityRelationnal>(ref entity, functionnalKey);

                    // ALL OK => commit transaction
                    transactionScope.Complete();
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex.Message);
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Get all
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TEntityRelationnal> GetAll()
        {
            return _sqlConnection.Query<TEntityRelationnal>(@$"SELECT * FROM [{typeof(THub).FindSchemaTableTarget()}].[v_{typeof(TEntityRelationnal).Name}]");
        }

        /// <summary>
        /// Compute foreign key name
        /// </summary>
        /// <param name="satelliteName"></param>
        /// <param name="HubName"></param>
        /// <returns></returns>
        private static string ExtractShortNameSatellite(string satelliteName, string HubName)
        {
            // Example : (SClientAdresse, Client) => SAdresseLdts

            return $"{satelliteName.Replace(HubName, string.Empty)}Ldts";
        }
    }
}
