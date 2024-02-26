namespace CustomORM.Abstractions;

public interface IRepository<TEntityRelationnal, THub>
    where TEntityRelationnal : class, new()
    where THub : class, new()
{

    /// <summary>
    /// Create a new DV entry
    /// </summary>
    /// <param name="entity">The entity to be inserted</param>
    void Insert(ref TEntityRelationnal entity);

    /// <summary>
    /// Get list
    /// </summary>
    /// <returns></returns>
    IEnumerable<TEntityRelationnal> GetAll();
}
