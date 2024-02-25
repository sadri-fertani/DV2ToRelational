namespace CustomORM.Abstractions;

public interface IRepository<TEntityRelationnal> where TEntityRelationnal : class
{
    /// <summary>
    /// Create a new DV entry
    /// </summary>
    /// <param name="entity">The entity to be inserted</param>
    /// <returns>Same entities with a set primary key and set audit attributes</returns>
    void Insert(ref TEntityRelationnal entity);
}
