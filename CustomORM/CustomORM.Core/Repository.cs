using CustomORM.Abstractions;
using CustomORM.Core.Extensions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace CustomORM.Core
{
    public sealed class Repository<TEntityRelationnal> : IRepository<TEntityRelationnal> where TEntityRelationnal : class
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
            var hub = (IHub)Activator.CreateInstance(Type.GetType($"{typeof(Repository<>).Namespace}.Entities.H{typeof(TEntityRelationnal).Name}")!)!;

            // 2- Insert key and hash
            hub.KeyHk = hashId;
            hub.FunctionnalKey = getNewId;

            // 3- insert into hub : ok nothing special
            string requestInsertHub = @$"INSERT INTO [dbo].[h_{typeof(TEntityRelationnal).Name.ToLower()}] VALUES ({Type.GetType($"{typeof(Repository<>).Namespace}.Entities.H{typeof(TEntityRelationnal).Name}")!.GetNamesColumns()})";

            var rowsAffected = _sqlConnection.Execute(
                requestInsertHub,
                hub.ConvertToParamsRequest());


            // map entity form Relationnal to DV2

            SpyIL.SetFunctionnalKey<TEntityRelationnal>(ref entity, getNewId);
        }

        /// <summary>
        /// Get new id from sequence
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private int GetNewFunctionnalKey(TEntityRelationnal entity)
        {
            return _sqlConnection.QueryFirst<int>($"SELECT NEXT VALUE FOR dbo.seq_h_{typeof(TEntityRelationnal).Name.ToLower()}");
        }
    }
}
