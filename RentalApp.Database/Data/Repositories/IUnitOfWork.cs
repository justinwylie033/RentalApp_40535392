namespace RentalApp.Database.Data.Repositories;

/// <summary>Defines the explicit commit boundary for one business operation.</summary>
public interface IUnitOfWork
{
    /// <summary>Commits all staged EF Core changes as one save operation.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
