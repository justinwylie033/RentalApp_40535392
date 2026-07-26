namespace RentalApp.Api.Services;

/// <summary>Adds defence-in-depth headers to every API response.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            return Task.CompletedTask;
        });

        return next(context);
    }
}
