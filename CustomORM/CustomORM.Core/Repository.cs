using CustomORM.Abstractions;
using CustomORM.Core.Extensions;
using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;
using System.Reflection;
using System.Transactions;
using static Dapper.SqlMapper;

namespace CustomORM.Core;

public sealed class Repository<TEntityRelationnal, TEntityHub, TFunctionalKeyType> : IRepository<TEntityRelationnal, TEntityHub, TFunctionalKeyType>
    where TEntityRelationnal : class, new()
    where TEntityHub : class, new()
    where TFunctionalKeyType : IConvertible
{
    private SqlConnection SqlConnection { get; set; }

    public Repository(SqlConnection sqlConnection)
    {
        this.SqlConnection = sqlConnection;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    /// <summary>
    ///  Create a new DV entry
    /// </summary>
    /// <param name="entity">The entity to be inserted</param>
    /// <param name="functionnalKey"></param>
    /// <exception cref="Exception"></exception>
    public void Add(ref TEntityRelationnal entity, Func<TFunctionalKeyType> GetFunctionalKey)
    {
        using var transactionScope = new TransactionScope();
        try
        {
            // Audit infos
            AuditInformations auditInfos = new();

            // New functionnal id => hash256
            var functionnalKey = GetFunctionalKey.Invoke();
            var hashId = functionnalKey.ToSha256();

            // 1- New instance of hub
            AddHub(ref entity, auditInfos, functionnalKey, hashId);

            // 4- insert into Satellites            
            AddSatellites(ref entity, auditInfos, hashId, out Dictionary<string, DateTime> satelliteLdtsForeignKeys);

            // 5- insert into PIT
            AddPit(ref entity, auditInfos, hashId, satelliteLdtsForeignKeys);

            // Inject functional key into entity
            PropertiesExtractorExtensions.SetFunctionnalKey<TEntityRelationnal, TFunctionalKeyType>(ref entity, functionnalKey);

            // ALL OK => commit transaction
            transactionScope.Complete();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex.Message);
            Console.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Get all
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TEntityRelationnal> GetAll()
    {
        // TODO : Remove top 10
        return SqlConnection.Query<TEntityRelationnal>(@$"SELECT TOP 10 * FROM [{typeof(TEntityHub).FindTableTargetInformation(TableSqlInfos.Schema)}].[v_{typeof(TEntityRelationnal).Name}]");
    }

    /// <summary>
    /// Find one entity
    /// </summary>
    /// <param name="functionalKey"></param>
    /// <returns></returns>
    public TEntityRelationnal? Get(TFunctionalKeyType functionalKey)
    {
        return SqlConnection.QueryFirstOrDefault<TEntityRelationnal>
            (
                @$"SELECT * FROM [{typeof(TEntityHub).FindTableTargetInformation(TableSqlInfos.Schema)}].[v_{typeof(TEntityRelationnal).Name}] WHERE [{typeof(TEntityHub).FindColumnName(typeof(TEntityRelationnal).FindKey())}] = @{nameof(functionalKey)}",
                new { functionalKey = functionalKey }
            );
    }

    /// <summary>
    /// Update entity
    /// </summary>
    /// <param name="entity"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Update(ref TEntityRelationnal entity)
    {
        #region check entity exist
        var functionalKey = (TFunctionalKeyType)Convert.ChangeType(entity.GetValue(entity.FindKey()), typeof(TFunctionalKeyType));
        var entityFromDb = Get(functionalKey);

        if (entityFromDb == null)
            throw new Exception("Not found");
        #endregion

        // Audit infos
        AuditInformations auditInfos = new();

        using var transactionScope = new TransactionScope();
        try
        {
            // insert new sattelite
            AddSatellites(ref entity, auditInfos, functionalKey.ToSha256(), out Dictionary<string, DateTime> satelliteLdtsForeignKeys);

            // call Delete with same date creation of satellites
            Delete(functionalKey, auditInfos.LoadDts);

            // insert new pit
            AddPit(ref entity, auditInfos, functionalKey.ToSha256(), satelliteLdtsForeignKeys);

            // ALL OK => commit transaction
            transactionScope.Complete();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex.Message);
            Console.WriteLine(ex.Message);
        }
    }

    /// <summary>
    /// Delete entity
    /// </summary>
    /// <param name="functionalKey"></param>
    /// <param name="dateDeleteSynchro"></param>
    public void Delete(TFunctionalKeyType functionalKey, DateTime? dateDeleteSynchro = null)
    {
        SqlConnection.Query
            (
                @$"UPDATE [{typeof(TEntityHub).FindTableTargetInformation(TableSqlInfos.Schema)}].[p_{typeof(TEntityRelationnal).Name}] SET [p_load_end_dts] = @currentDate WHERE [{typeof(TEntityHub).FindColumnName(typeof(TEntityHub).FindKey())}] = @hKey AND [p_load_end_dts] IS NULL",
                new
                {
                    currentDate = dateDeleteSynchro ?? DateTime.Now,
                    hKey = functionalKey.ToSha256()
                }
            );
    }

    /// <summary>
    /// Add satellite(s)
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="auditInformations"></param>
    /// <param name="hashId"></param>
    /// <param name="satelliteLdtsForeignKeys"></param>
    /// <exception cref="Exception"></exception>
    private void AddSatellites(ref TEntityRelationnal entity, AuditInformations auditInformations, string hashId, out Dictionary<string, DateTime> satelliteLdtsForeignKeys)
    {
        // Foreign key between Satellite and Pit
        satelliteLdtsForeignKeys = [];

        foreach (var propSat in typeof(TEntityHub).GetListOfChildrenObjects(DV2TypeObject.S))
        {
            // 1- new instance of Satellite
            var satellite = PropertiesExtractorExtensions.GetInstance(propSat.FullName!);

            // 2- Insert key and hash
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref satellite, satellite.FindKey(), hashId);

            // 3.1- Inject audit infos to satellite
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref satellite, nameof(ISatellite.SLoadDts), auditInformations.LoadDts);
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref satellite, nameof(ISatellite.SLoadUser), auditInformations.LoadUser);
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref satellite, nameof(ISatellite.SLoadSrc), auditInformations.LoadSrc);

            // 3.2- Save foreign key
            satelliteLdtsForeignKeys.Add(
                $"{propSat.Name.Replace(typeof(TEntityRelationnal).Name, string.Empty)}Ldts",
                auditInformations.LoadDts);

            // 4- Load from Dto
            PropertiesExtractorExtensions.ChargerSatellite(ref satellite!, entity, typeof(TEntityHub).Namespace!);

            // 5- Insert into db
            Type typeSatellite = Assembly.GetEntryAssembly()!.GetTypes().Where(t => t.FullName == propSat.FullName).First();

            var rowsSatelliteAffected = SqlConnection.Execute(
                @$"INSERT INTO [{typeSatellite.FindTableTargetInformation(TableSqlInfos.Schema)}].[{typeSatellite.FindTableTargetInformation(TableSqlInfos.Name)}] VALUES ({typeSatellite.GetNamesColumns()})",
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

    /// <summary>
    /// Add Pit
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="auditInformations"></param>
    /// <param name="hashId"></param>
    /// <param name="satelliteLdtsForeignKeys"></param>
    /// <exception cref="Exception"></exception>
    private void AddPit(ref TEntityRelationnal entity, AuditInformations auditInformations, string hashId, Dictionary<string, DateTime> satelliteLdtsForeignKeys)
    {
        var propPit = typeof(TEntityHub).GetListOfChildrenObjects(DV2TypeObject.P).FirstOrDefault();

        if (propPit != null)
        {
            // 1- new instance of PIT
            var pit = PropertiesExtractorExtensions.GetInstance(propPit.FullName!);

            // 2- Insert key and hash
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref pit, pit.FindKey(), hashId);

            // 3- Inject audit infos to pit
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref pit, nameof(IPit.PLoadDts), auditInformations.LoadDts);
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref pit, nameof(IPit.PLoadEndDts));
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref pit, nameof(IPit.PLoadUser), auditInformations.LoadUser);
            PropertiesExtractorExtensions.SetObjectProperty<object>(ref pit, nameof(IPit.PLoadSrc), auditInformations.LoadSrc);

            // 4- Inject foreigns keys
            foreach (var kv in satelliteLdtsForeignKeys)
                PropertiesExtractorExtensions.SetObjectProperty<object>(ref pit, kv.Key, kv.Value);

            // 5- Insert into db
            Type typePit = Assembly.GetEntryAssembly()!.GetTypes().Where(t => t.FullName == propPit.FullName).First();

            var rowsPitAffected = SqlConnection.Execute(
                @$"INSERT INTO [{typePit.FindTableTargetInformation(TableSqlInfos.Schema)}].[{typePit.FindTableTargetInformation(TableSqlInfos.Name)}] VALUES ({typePit.GetNamesColumns()})",
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

    /// <summary>
    /// Add hub
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="auditInformations"></param>
    /// <param name="functionnalKey"></param>
    /// <param name="hashId"></param>
    /// <exception cref="Exception"></exception>
    private void AddHub(ref TEntityRelationnal entity, AuditInformations auditInformations, TFunctionalKeyType functionnalKey, string hashId)
    {
        var hub = Activator.CreateInstance(typeof(TEntityHub))! as TEntityHub;

        if (hub == null)
            throw new Exception("Error create an instance of hub");

        // 2- Insert key and hash
        PropertiesExtractorExtensions.SetObjectProperty<TEntityHub>(ref hub, typeof(TEntityHub).FindKey(), hashId);
        PropertiesExtractorExtensions.SetObjectProperty<TEntityHub>(ref hub, typeof(TEntityRelationnal).FindKey(), functionnalKey);// Construction : TEntityRelationnal has one key : It's the same functionnal key in THub

        // Inject audit infos to hub
        PropertiesExtractorExtensions.SetObjectProperty<TEntityHub>(ref hub, nameof(IHub.HLoadDts), auditInformations.LoadDts);
        PropertiesExtractorExtensions.SetObjectProperty<TEntityHub>(ref hub, nameof(IHub.HLoadUser), auditInformations.LoadUser);
        PropertiesExtractorExtensions.SetObjectProperty<TEntityHub>(ref hub, nameof(IHub.HLoadSrc), auditInformations.LoadSrc);

        // 3- insert into hub        
        var rowsHubAffected = SqlConnection.Execute(
            @$"INSERT INTO [{typeof(TEntityHub).FindTableTargetInformation(TableSqlInfos.Schema)}].[{typeof(TEntityHub).FindTableTargetInformation(TableSqlInfos.Name)}] VALUES ({typeof(TEntityHub)!.GetNamesColumns()})",
            hub.ConvertToParamsRequest(),
            commandType: System.Data.CommandType.Text);

        if (rowsHubAffected > 0)
            Log.Information("Insertion HUB : OK");
        else
        {
            Log.Fatal("ERROR INSERT HUB");
            throw new Exception("ERROR INSERT HUB");
        }
    }
}
