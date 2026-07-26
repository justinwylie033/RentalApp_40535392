namespace RentalApp.Database.Data.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    // Presentation point: services decide when a complete use case commits, keeping
    // several repository changes within one EF Core SaveChanges transaction.
    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
