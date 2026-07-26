using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using RentalApp.Contracts;
using RentalApp.Database.Models;

namespace RentalApp.Database.Data.Repositories;

public sealed class ItemRepository : Repository<Item>, IItemRepository
{
    public ItemRepository(AppDbContext context)
        : base(context)
    {
    }

    public async Task<(IReadOnlyList<Item> Items, int TotalCount)> GetAvailableAsync(
        ItemCategory? category,
        string? search,
        ItemSortOrder sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Item> query = Context.Items
            .AsNoTracking()
            .Include(item => item.Owner)
            .Include(item => item.Reviews)
            .Where(item => item.IsAvailable);

        if (category is not null)
        {
            query = query.Where(item => item.Category == category);
        }

        var normalizedSearch = search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(item =>
                item.Title.ToLower().Contains(normalizedSearch)
                || item.Description.ToLower().Contains(normalizedSearch)
                || item.Address.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = sort switch
        {
            ItemSortOrder.PriceLowToHigh => query
                .OrderBy(item => item.DailyRate)
                .ThenByDescending(item => item.CreatedAtUtc),
            ItemSortOrder.PriceHighToLow => query
                .OrderByDescending(item => item.DailyRate)
                .ThenByDescending(item => item.CreatedAtUtc),
            ItemSortOrder.RatingHighToLow => query
                .OrderByDescending(item => item.Reviews.Count == 0
                    ? 0
                    : item.Reviews.Average(review => review.Rating))
                .ThenByDescending(item => item.CreatedAtUtc),
            _ => query.OrderByDescending(item => item.CreatedAtUtc)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Item?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await Context.Items
            .AsNoTracking()
            .Include(item => item.Owner)
            .Include(item => item.Reviews)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return item;
    }

    public async Task<IReadOnlyList<Item>> GetOwnedAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Items
            .AsNoTracking()
            .Include(item => item.Owner)
            .Include(item => item.Reviews)
            .Where(item => item.OwnerId == ownerId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<(Item Item, double DistanceMetres)>> GetNearbyAsync(
        double latitude,
        double longitude,
        double radiusKilometres,
        ItemCategory? category,
        CancellationToken cancellationToken = default)
    {
        // Presentation point: longitude is X and latitude is Y. SRID 4326 identifies
        // WGS84 coordinates, while the geography column makes distances metres.
        var origin = new Point(longitude, latitude) { SRID = 4326 };
        var radiusMetres = radiusKilometres * 1_000;

        IQueryable<Item> query = Context.Items
            .AsNoTracking()
            .Include(item => item.Owner)
            .Include(item => item.Reviews)
            // Npgsql translates IsWithinDistance to PostGIS ST_DWithin, allowing the
            // GiST index to filter on the database server instead of in application memory.
            .Where(item => item.IsAvailable && item.Location.IsWithinDistance(origin, radiusMetres));

        if (category is not null)
        {
            query = query.Where(item => item.Category == category);
        }

        // Distance is projected and sorted in SQL so the closest listing is first.
        var results = await query
            .Select(item => new
            {
                Item = item,
                DistanceMetres = item.Location.Distance(origin)
            })
            .OrderBy(result => result.DistanceMetres)
            .ToListAsync(cancellationToken);
        var nearbyItems = results
            .Select(result => (result.Item, result.DistanceMetres))
            .ToList();

        return nearbyItems;
    }
}
