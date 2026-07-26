using RentalApp.Contracts;

namespace RentalApp.Application.Services;

/// <summary>Defines item catalogue operations used by mobile ViewModels.</summary>
public interface IItemService
{
    /// <summary>Returns available listings, optionally filtered by category.</summary>
    Task<IReadOnlyList<ItemSummaryDto>> GetAllAsync(ItemCategory? category = null, CancellationToken cancellationToken = default);
    /// <summary>Returns listings inside a radius of the supplied position.</summary>
    Task<IReadOnlyList<ItemSummaryDto>> FindNearbyAsync(
        double latitude,
        double longitude,
        double radiusKilometres,
        ItemCategory? category = null,
        CancellationToken cancellationToken = default);
    /// <summary>Returns full details for one listing.</summary>
    Task<ItemDetailDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Publishes a new listing owned by the signed-in user.</summary>
    Task<ItemDetailDto> CreateAsync(CreateItemRequest request, CancellationToken cancellationToken = default);
    /// <summary>Updates a listing through the owner-authorised API.</summary>
    Task<ItemDetailDto> UpdateAsync(Guid id, UpdateItemRequest request, CancellationToken cancellationToken = default);
}

public sealed class ItemService(IApiClient api) : IItemService
{
    public Task<IReadOnlyList<ItemSummaryDto>> GetAllAsync(
        ItemCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var path = category is null ? "items/" : $"items/?category={category}";
        return api.GetAsync<IReadOnlyList<ItemSummaryDto>>(path, cancellationToken);
    }

    public Task<IReadOnlyList<ItemSummaryDto>> FindNearbyAsync(
        double latitude,
        double longitude,
        double radiusKilometres,
        ItemCategory? category = null,
        CancellationToken cancellationToken = default)
    {
        var path = FormattableString.Invariant(
            $"items/nearby?latitude={latitude}&longitude={longitude}&radiusKm={radiusKilometres}");
        if (category is not null)
        {
            path += $"&category={category}";
        }

        return api.GetAsync<IReadOnlyList<ItemSummaryDto>>(path, cancellationToken);
    }

    public Task<ItemDetailDto> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        api.GetAsync<ItemDetailDto>($"items/{id}", cancellationToken);

    public Task<ItemDetailDto> CreateAsync(CreateItemRequest request, CancellationToken cancellationToken = default) =>
        api.PostAsync<CreateItemRequest, ItemDetailDto>("items/", request, cancellationToken);

    public Task<ItemDetailDto> UpdateAsync(
        Guid id,
        UpdateItemRequest request,
        CancellationToken cancellationToken = default) =>
        api.PutAsync<UpdateItemRequest, ItemDetailDto>($"items/{id}", request, cancellationToken);
}
