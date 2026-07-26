namespace RentalApp.Database.Data.Repositories;

/// <summary>Defines shared persistence operations for aggregate repositories.</summary>
public interface IRepository<T> where T : class
{
    /// <summary>Finds one entity by its identifier.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Returns all entities represented by this repository.</summary>
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>Stages a new entity for insertion.</summary>
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    /// <summary>Marks an existing entity as changed.</summary>
    void Update(T entity);
    /// <summary>Marks an existing entity for removal.</summary>
    void Remove(T entity);
}
