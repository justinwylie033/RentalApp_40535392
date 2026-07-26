using NetTopologySuite.Geometries;
using RentalApp.Contracts;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;

namespace RentalApp.Api.Services;

public interface IItemApplicationService
{
    Task<PagedResult<ItemSummaryDto>> GetAllAsync(
        ItemCategory? category,
        string? search,
        ItemSortOrder sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ItemSummaryDto>> GetOwnedAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<ItemDetailDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ItemDetailDto> CreateAsync(Guid ownerId, CreateItemRequest request, CancellationToken cancellationToken = default);
    Task<ItemDetailDto> UpdateAsync(Guid ownerId, Guid id, UpdateItemRequest request, CancellationToken cancellationToken = default);
}

public sealed class ItemApplicationService : IItemApplicationService
{
    private readonly IItemRepository _items;
    private readonly IUnitOfWork _unitOfWork;

    // Presentation point: this is the Service Layer for item use cases. Validation
    // here protects the API even when a client bypasses the MAUI user interface.
    public ItemApplicationService(IItemRepository items, IUnitOfWork unitOfWork)
    {
        _items = items;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<ItemSummaryDto>> GetAllAsync(
        ItemCategory? category,
        string? search,
        ItemSortOrder sort,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (search?.Trim().Length > 100)
        {
            throw new BusinessRuleException("Search text cannot exceed 100 characters.");
        }

        if (page < 1)
        {
            throw new BusinessRuleException("Page must be at least 1.");
        }

        if (pageSize is < 1 or > 50)
        {
            throw new BusinessRuleException("Page size must be between 1 and 50.");
        }

        var result = await _items.GetAvailableAsync(
            category,
            search,
            sort,
            page,
            pageSize,
            cancellationToken);
        var items = result.Items.Select(item => item.ToSummary()).ToList();
        return new PagedResult<ItemSummaryDto>(items, page, pageSize, result.TotalCount);
    }

    public async Task<ItemDetailDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _items.GetDetailsAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Item not found.");
        return item.ToDetail();
    }

    public async Task<IReadOnlyList<ItemSummaryDto>> GetOwnedAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var ownedItems = await _items.GetOwnedAsync(ownerId, cancellationToken);
        return ownedItems.Select(item => item.ToSummary()).ToList();
    }

    public async Task<ItemDetailDto> CreateAsync(
        Guid ownerId,
        CreateItemRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.Title, request.Description, request.DailyRate, request.Address, request.Latitude, request.Longitude);
        var item = new Item
        {
            OwnerId = ownerId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DailyRate = request.DailyRate,
            Category = request.Category,
            Address = request.Address.Trim(),
            // NetTopologySuite uses X=longitude and Y=latitude.
            Location = CreatePoint(request.Latitude, request.Longitude)
        };

        await _items.AddAsync(item, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetAsync(item.Id, cancellationToken);
    }

    public async Task<ItemDetailDto> UpdateAsync(
        Guid ownerId,
        Guid id,
        UpdateItemRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request.Title, request.Description, request.DailyRate, request.Address, request.Latitude, request.Longitude);
        var item = await _items.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Item not found.");

        if (item.OwnerId != ownerId)
        {
            throw new UnauthorizedAccessException("Only the owner can update this item.");
        }

        item.Title = request.Title.Trim();
        item.Description = request.Description.Trim();
        item.DailyRate = request.DailyRate;
        item.Category = request.Category;
        item.Address = request.Address.Trim();
        item.Location = CreatePoint(request.Latitude, request.Longitude);
        item.IsAvailable = request.IsAvailable;
        _items.Update(item);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetAsync(item.Id, cancellationToken);
    }

    private static Point CreatePoint(double latitude, double longitude)
    {
        var point = new Point(longitude, latitude)
        {
            SRID = 4326
        };

        return point;
    }

    private static void Validate(
        string title,
        string description,
        decimal rate,
        string address,
        double latitude,
        double longitude)
    {
        if (title.Trim().Length is < 3 or > 120)
        {
            throw new BusinessRuleException("Title must contain between 3 and 120 characters.");
        }

        if (description.Trim().Length is < 10 or > 1_500)
        {
            throw new BusinessRuleException("Description must contain between 10 and 1,500 characters.");
        }

        if (rate is <= 0 or > 10_000)
        {
            throw new BusinessRuleException("Daily rate must be greater than zero and no more than £10,000.");
        }

        if (string.IsNullOrWhiteSpace(address) || address.Trim().Length is < 5 or > 250)
        {
            throw new BusinessRuleException("Enter a collection address between 5 and 250 characters.");
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new BusinessRuleException("Location coordinates are outside the valid range.");
        }
    }
}

internal static class ItemMappingExtensions
{
    // Presentation point: DTO mapping prevents EF entities and navigation properties
    // from leaking across the HTTP boundary to the mobile application.
    public static ItemSummaryDto ToSummary(this Item item, double? distanceKm = null)
    {
        var averageRating = GetAverageRating(item);

        return new ItemSummaryDto(
            item.Id,
            item.OwnerId,
            item.Owner.DisplayName,
            item.Title,
            item.DailyRate,
            item.Category,
            item.Location.Y,
            item.Location.X,
            item.IsAvailable,
            averageRating,
            item.Reviews.Count,
            distanceKm,
            item.Address);
    }

    public static ItemDetailDto ToDetail(this Item item)
    {
        var averageRating = GetAverageRating(item);

        return new ItemDetailDto(
            item.Id,
            item.OwnerId,
            item.Owner.DisplayName,
            item.Title,
            item.Description,
            item.DailyRate,
            item.Category,
            item.Location.Y,
            item.Location.X,
            item.IsAvailable,
            averageRating,
            item.Reviews.Count,
            item.CreatedAtUtc,
            item.Address);
    }

    private static double GetAverageRating(Item item)
    {
        if (item.Reviews.Count == 0)
        {
            return 0;
        }

        return item.Reviews.Average(review => review.Rating);
    }
}
