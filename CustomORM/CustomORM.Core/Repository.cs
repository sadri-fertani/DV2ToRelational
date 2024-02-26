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

namespace CustomORM.Core
{
    public sealed class Repository<TEntityRelationnal, THub, TSatellite1> : IRepository<TEntityRelationnal, THub>
        where TEntityRelationnal : class, new()
        where THub : class, new()
    {
        private SqlConnection _sqlConnection { get; set; }

        public Repository(SqlConnection sqlConnection)
        {
            this._sqlConnection = sqlConnection;
        }

        /// <summary>
        /// Create a new DV entry
        /// </summary>
        /// <param name="entity">The entity to be inserted</param>
        /// <returns>Same entities with a set primary key and set audit attributes</returns>
        public void Insert(ref TEntityRelationnal entity)
        {
            // Audit infos
            var LoadDts = DateTime.Now;
            var LoadUser = Environment.UserName;
            var LoadSrc = AppDomain.CurrentDomain.FriendlyName;

            // New functionnal id => hash256
            var getNewId = GetNewFunctionnalKey(entity);
            var hashId = getNewId.ToSha256();

            // 1- New instance of hub
            var hub = Activator.CreateInstance(typeof(THub))! as THub;

            // 2- Insert key and hash
            SpyIL.SetAuditInfo<THub>(ref hub, hub.FindKey<THub>(), hashId);
            SpyIL.SetAuditInfo<THub>(ref hub, entity.FindKey<TEntityRelationnal>(), getNewId);// Construction : TEntityRelationnal has one key : It's the same functionnal key in THub

            // Inject audit infos to hub
            SpyIL.SetAuditInfo<THub>(ref hub, nameof(IHub.HLoadDts), LoadDts);
            SpyIL.SetAuditInfo<THub>(ref hub, nameof(IHub.HLoadUser), LoadUser);
            SpyIL.SetAuditInfo<THub>(ref hub, nameof(IHub.HLoadSrc), LoadSrc);

            // 3- insert into hub        
            var rowsAffected = _sqlConnection.Query<THub>(
                @$"INSERT INTO [dbo].[{typeof(THub).FindTableTarget()}] VALUES ({typeof(THub)!.GetNamesColumns()})",
                hub.ConvertToParamsRequest(),
                commandType: System.Data.CommandType.Text);

            if (rowsAffected.Any()) 
            {
                Log.Fatal("ERROR INSERT HUB");
                throw new Exception("ERROR INSERT HUB");
            }
            else
                Log.Information("Insertion HUB : OK");

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
                    Type typeSatellite = Satellite.GetType();
                    SpyIL.SetAuditInfo<object> (ref Satellite, hub.FindKey<THub>(), hashId);
                    
                    // Inject audit infos to hub
                    SpyIL.SetAuditInfo<object>(ref Satellite, nameof(ISatellite.SLoadDts), LoadDts);
                    SpyIL.SetAuditInfo<object>(ref Satellite, nameof(ISatellite.SLoadUser), LoadUser);
                    SpyIL.SetAuditInfo<object>(ref Satellite, nameof(ISatellite.SLoadSrc), LoadSrc);

                    // Load from Dto
                    SpyIL.ChargerSatellite(Satellite!, hub, entity, typeof(THub).Namespace!);
                }
            }

            // Inject functional key into entity
            SpyIL.SetFunctionnalKey<TEntityRelationnal>(ref entity, getNewId);
        }

        /// <summary>
        /// Get new id from sequence
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private int GetNewFunctionnalKey(TEntityRelationnal entity)
        {
            return _sqlConnection.QueryFirst<int>($"SELECT NEXT VALUE FOR dbo.seq_{typeof(THub).FindTableTarget()}");
        }
    }
}
