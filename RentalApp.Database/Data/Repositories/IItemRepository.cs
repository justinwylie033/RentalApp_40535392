using RentalApp.Contracts;
using RentalApp.Database.Models;

namespace RentalApp.Database.Data.Repositories;

/// <summary>Provides item-specific catalogue and PostGIS queries.</summary>
public interface IItemRepository : IRepository<Item>
{
    /// <summary>Returns available items, optionally restricted to one category.</summary>
    Task<IReadOnlyList<Item>> GetAvailableAsync(ItemCategory? category, CancellationToken cancellationToken = default);
    /// <summary>Returns one item with the relationships needed by the API.</summary>
    Task<Item?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Runs the indexed PostGIS radius query and returns distances in metres.</summary>
    Task<IReadOnlyList<(Item Item, double DistanceMetres)>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKilometres,
        ItemCategory? category,
        CancellationToken cancellationToken = default);
}
