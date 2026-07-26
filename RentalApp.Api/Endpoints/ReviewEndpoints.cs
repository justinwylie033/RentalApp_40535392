using Microsoft.AspNetCore.Mvc;
using RentalApp.Api.Services;
using RentalApp.Contracts;

namespace RentalApp.Api.Endpoints;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/reviews").WithTags("Reviews").RequireAuthorization();
        group.MapGet("/items/{itemId:guid}", GetForItemAsync);
        group.MapPost("/", CreateAsync);
        return endpoints;
    }

    private static async Task<IResult> GetForItemAsync(
        Guid itemId,
        IReviewService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetForItemAsync(itemId, cancellationToken));

    private static async Task<IResult> CreateAsync(
        [FromBody] CreateReviewRequest request,
        HttpContext context,
        IReviewService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(context.User.GetUserId(), request, cancellationToken);
        return Results.Created($"/reviews/{result.Id}", result);
    }
}
