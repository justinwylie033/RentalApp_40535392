namespace RentalApp.Contracts;

public sealed record ItemSummaryDto(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string Title,
    decimal DailyRate,
    ItemCategory Category,
    double Latitude,
    double Longitude,
    bool IsAvailable,
    double AverageRating,
    int ReviewCount,
    double? DistanceKm,
    string Address = "Location not specified");

public sealed record ItemDetailDto(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string Title,
    string Description,
    decimal DailyRate,
    ItemCategory Category,
    double Latitude,
    double Longitude,
    bool IsAvailable,
    double AverageRating,
    int ReviewCount,
    DateTimeOffset CreatedAtUtc,
    string Address = "Location not specified");

public sealed record CreateItemRequest(
    string Title,
    string Description,
    decimal DailyRate,
    ItemCategory Category,
    double Latitude,
    double Longitude,
    string Address = "Location not specified");

public sealed record UpdateItemRequest(
    string Title,
    string Description,
    decimal DailyRate,
    ItemCategory Category,
    double Latitude,
    double Longitude,
    bool IsAvailable,
    string Address = "Location not specified");
