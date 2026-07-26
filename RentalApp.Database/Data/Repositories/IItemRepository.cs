using RentalApp.Contracts;
using RentalApp.Database.Models;

namespace RentalApp.Database.Data.Repositories;

/// <summary>Provides item-specific catalogue and PostGIS queries.</summary>
public interface IItemRepository : IRepository<Item>
{
    /// <summary>Returns available items using the supplied catalogue filters and ordering.</summary>
    Task<(IReadOnlyList<Item> Items, int TotalCount)> GetAvailableAsync(
        ItemCategory? category,
        string? search,
        ItemSortOrder sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    /// <summary>Returns every listing belonging to one account, including unavailable listings.</summary>
    Task<IReadOnlyList<Item>> GetOwnedAsync(Guid ownerId, CancellationToken cancellationToken = default);
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
