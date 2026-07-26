using Microsoft.AspNetCore.Mvc;
using RentalApp.Api.Services;
using RentalApp.Contracts;

namespace RentalApp.Api.Endpoints;

public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Presentation point: Minimal API endpoints are deliberately thin. They deal
        // with HTTP concerns and delegate rules to the application service.
        var group = endpoints.MapGroup("/items").WithTags("Items").RequireAuthorization();
        group.MapGet("/", GetAllAsync);
        group.MapGet("/nearby", GetNearbyAsync);
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:guid}", UpdateAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAllAsync(
        [FromQuery] ItemCategory? category,
        [FromQuery] string? search,
        [FromQuery] ItemSortOrder sort,
        IItemApplicationService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetAllAsync(category, search, sort, cancellationToken));

    private static async Task<IResult> GetNearbyAsync(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] double radiusKm,
        [FromQuery] ItemCategory? category,
        ILocationService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.FindNearbyAsync(latitude, longitude, radiusKm, category, cancellationToken));

    private static async Task<IResult> GetAsync(
        Guid id,
        IItemApplicationService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetAsync(id, cancellationToken));

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateItemRequest request,
        HttpContext context,
        IItemApplicationService service,
        CancellationToken cancellationToken)
    {
        // The authenticated user id comes from the validated JWT, never from the body.
        var result = await service.CreateAsync(context.User.GetUserId(), request, cancellationToken);
        return Results.Created($"/items/{result.Id}", result);
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        [FromBody] UpdateItemRequest request,
        HttpContext context,
        IItemApplicationService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.UpdateAsync(context.User.GetUserId(), id, request, cancellationToken));
}
