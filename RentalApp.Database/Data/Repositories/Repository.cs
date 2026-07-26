using Microsoft.EntityFrameworkCore;

namespace RentalApp.Database.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    // Presentation point: the generic repository handles shared CRUD behaviour;
    // specialised repositories add business-specific queries without duplication.
    protected AppDbContext Context { get; }
    protected DbSet<T> Entities { get; }

    public Repository(AppDbContext context)
    {
        Context = context;
        Entities = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await Entities.FindAsync(new object[] { id }, cancellationToken);
        return entity;
    }

    public virtual async Task<IReadOnlyList<T>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        // Read-only queries avoid change-tracking overhead.
        return await Entities.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        Entities.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        Entities.Remove(entity);
    }
}
