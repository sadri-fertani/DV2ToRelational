namespace CustomORM.Abstractions;

public interface IRepository<TEntityRelationnal, THub, TFunctionalKeyType>
    where TEntityRelationnal : class, new()
    where THub : class, new()
    where TFunctionalKeyType : IConvertible
{

    /// <summary>
    /// Create a new DV entry
    /// </summary>
    /// <param name="entity">The entity to be inserted</param>
    /// <param name="GetFunctionalKey">Function to get a functional key defined by caller</param>
    void Add(ref TEntityRelationnal entity, Func<TFunctionalKeyType> GetFunctionalKey);

    /// <summary>
    /// Get list
    /// </summary>
    /// <returns></returns>
    IEnumerable<TEntityRelationnal> GetAll();

    /// <summary>
    /// Get one
    /// </summary>
    /// <returns></returns>
    TEntityRelationnal Get(TFunctionalKeyType functionalKey);

    /// <summary>
    /// Update entity
    /// </summary>
    /// <param name="entity"></param>
    void Update(ref TEntityRelationnal entity);

    /// <summary>
    /// Delete entity
    /// </summary>
    /// <param name="functionalKey"></param>
    /// <param name="dateDeleteSynchro"></param>
    void Delete(TFunctionalKeyType functionalKey, DateTime? dateDeleteSynchro = null);
}