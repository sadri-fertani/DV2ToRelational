using CustomORM.Abstractions;
using CustomORM.Core.Extensions;
using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

            // 4- insert into Satellite
            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(typeof(THub)!))
            {
                if (
                    prop.PropertyType.FullName!.Contains(typeof(THub)!.Namespace!) &&
                    (prop.PropertyType.Name == "ICollection`1") &&
                    prop.Name.StartsWith("S"))
                {
                    Console.WriteLine($"Collection of : {prop.PropertyType.GenericTypeArguments.First().FullName}");

                    // Satellite
                    var Satellite = SpyIL.GetInstance(prop.PropertyType.GenericTypeArguments.First().FullName!);

                    // 2- Insert key and hash
                    SpyIL.SetAuditInfo<object>(ref Satellite, hub.FindKey<THub>(), hashId);

                    // 3- Inject audit infos to hub
                    SpyIL.SetAuditInfo<object>(ref Satellite, nameof(ISatellite.SLoadDts), LoadDts);
                    SpyIL.SetAuditInfo<object>(ref Satellite, nameof(ISatellite.SLoadUser), LoadUser);
                    SpyIL.SetAuditInfo<object>(ref Satellite, nameof(ISatellite.SLoadSrc), LoadSrc);

                    // 4- Load from Dto
                    SpyIL.ChargerSatellite(ref Satellite!, hub, entity, typeof(THub).Namespace!);

                    // 5- Insert into db
                    Type typeSatellite = Assembly.GetEntryAssembly()!.GetTypes().Where(t => t.FullName == prop.PropertyType.GenericTypeArguments.First().FullName).First();

                    var rowsSatelliteAffected = _sqlConnection.Execute(
                        @$"INSERT INTO [{typeSatellite.FindSchemaTableTarget()}].[{typeSatellite.FindTableTarget()}] VALUES ({typeSatellite.GetNamesColumns()})",
                        Satellite.ConvertToParamsRequest(typeSatellite),
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

            // Inject functional key into entity
            SpyIL.SetFunctionnalKey<TEntityRelationnal>(ref entity, functionnalKey);
        }

        public IEnumerable<TEntityRelationnal> GetAll()
        {
            return _sqlConnection.Query<TEntityRelationnal>(@$"SELECT * FROM [{typeof(THub).FindSchemaTableTarget()}].[v_{typeof(TEntityRelationnal).Name}]");
        }
    }
}
