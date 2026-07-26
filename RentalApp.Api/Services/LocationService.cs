using RentalApp.Contracts;
using RentalApp.Database.Data.Repositories;

namespace RentalApp.Api.Services;

/// <summary>Validates and coordinates public location-search use cases.</summary>
public interface ILocationService
{
    /// <summary>Finds available listings within a validated geographic radius.</summary>
    Task<IReadOnlyList<ItemSummaryDto>> FindNearbyAsync(
        double latitude,
        double longitude,
        double radiusKilometres,
        ItemCategory? category,
        CancellationToken cancellationToken = default);
}

public sealed class LocationService(IItemRepository items) : ILocationService
{
    // Presentation point: the service validates public API input and converts the
    // repository's metre-based spatial result into UI-friendly kilometres.
    public async Task<IReadOnlyList<ItemSummaryDto>> FindNearbyAsync(
        double latitude,
        double longitude,
        double radiusKilometres,
        ItemCategory? category,
        CancellationToken cancellationToken = default)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new BusinessRuleException("Location coordinates are outside the valid range.");
        }

        if (radiusKilometres is < 0.1 or > 100)
        {
            throw new BusinessRuleException("Search radius must be between 0.1 and 100 kilometres.");
        }

        var results = await items.GetNearbyAsync(
            latitude,
            longitude,
            radiusKilometres,
            category,
            cancellationToken);
        return results.Select(result => result.Item.ToSummary(result.DistanceMetres / 1_000)).ToList();
    }
}
