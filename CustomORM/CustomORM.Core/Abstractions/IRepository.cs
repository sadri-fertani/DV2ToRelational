using Microsoft.AspNetCore.JsonPatch;

namespace CustomORM.Core.Abstractions;

internal interface IRepository<TEntityRelationnal, THub, TFunctionalKeyType>
    where TEntityRelationnal : class, new()
    where THub : class, new()
    where TFunctionalKeyType : IConvertible
{
    /// <summary>
    /// Get list
    /// </summary>
    /// <returns></returns>
    IEnumerable<TEntityRelationnal> GetAll();

    /// <summary>
    /// Get one
    /// </summary>
    /// <param name="functionalKey"></param>
    /// <returns></returns>
    TEntityRelationnal Get(TFunctionalKeyType functionalKey);

    /// <summary>
    /// Create a new DV entry
    /// </summary>
    /// <param name="entity">The entity to be inserted</param>
    /// <param name="GetFunctionalKey">Function to get a functional key defined by caller</param>
    void Add(ref TEntityRelationnal entity, Func<TFunctionalKeyType> GetFunctionalKey);

    /// <summary>
    /// Update entity
    /// </summary>
    /// <param name="entity"></param>
    void Update(ref TEntityRelationnal entity);

    /// <summary>
    /// Path entity
    /// </summary>
    /// <param name="functionalKey"></param>
    /// <param name="patch"></param>
    void Patch(TFunctionalKeyType functionalKey, JsonPatchDocument<TEntityRelationnal> patch);

    /// <summary>
    /// Delete entity
    /// </summary>
    /// <param name="functionalKey"></param>
    /// <param name="dateDeleteSynchro"></param>
    void Delete(TFunctionalKeyType functionalKey, DateTime? dateDeleteSynchro = null);
}
