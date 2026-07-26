using Microsoft.AspNetCore.Mvc;
using RentalApp.Api.Services;
using RentalApp.Contracts;

namespace RentalApp.Api.Endpoints;

public static class RentalEndpoints
{
    public static IEndpointRouteBuilder MapRentalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/rentals").WithTags("Rentals").RequireAuthorization();
        group.MapPost("/", RequestAsync);
        group.MapGet("/incoming", GetIncomingAsync);
        group.MapGet("/outgoing", GetOutgoingAsync);
        group.MapPatch("/{id:guid}/status", UpdateStatusAsync);
        return endpoints;
    }

    private static async Task<IResult> RequestAsync(
        [FromBody] CreateRentalRequest request,
        HttpContext context,
        IRentalWorkflowService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RequestAsync(context.User.GetUserId(), request, cancellationToken);
        return Results.Created($"/rentals/{result.Id}", result);
    }

    private static async Task<IResult> GetIncomingAsync(
        HttpContext context,
        IRentalWorkflowService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetIncomingAsync(context.User.GetUserId(), cancellationToken));

    private static async Task<IResult> GetOutgoingAsync(
        HttpContext context,
        IRentalWorkflowService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetOutgoingAsync(context.User.GetUserId(), cancellationToken));

    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        [FromBody] UpdateRentalStatusRequest request,
        HttpContext context,
        IRentalWorkflowService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.TransitionAsync(context.User.GetUserId(), id, request.Status, cancellationToken));
}
