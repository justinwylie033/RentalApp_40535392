using Microsoft.AspNetCore.Mvc;
using RentalApp.Api.Services;
using RentalApp.Contracts;

namespace RentalApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Authentication");
        group.MapPost("/register", RegisterAsync).AllowAnonymous();
        group.MapPost("/token", LoginAsync).AllowAnonymous();
        group.MapPost("/refresh", RefreshAsync).AllowAnonymous();
        group.MapGet("/me", GetProfileAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        IAuthenticationService authentication,
        CancellationToken cancellationToken) =>
        Results.Created("/auth/me", await authentication.RegisterAsync(request, cancellationToken));

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        IAuthenticationService authentication,
        CancellationToken cancellationToken) =>
        Results.Ok(await authentication.LoginAsync(request, cancellationToken));

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenRequest request,
        IAuthenticationService authentication,
        CancellationToken cancellationToken) =>
        Results.Ok(await authentication.RefreshAsync(request, cancellationToken));

    private static async Task<IResult> GetProfileAsync(
        HttpContext context,
        IAuthenticationService authentication,
        CancellationToken cancellationToken) =>
        Results.Ok(await authentication.GetProfileAsync(context.User.GetUserId(), cancellationToken));
}
