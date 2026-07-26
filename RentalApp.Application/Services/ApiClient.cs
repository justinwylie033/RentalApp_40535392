using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using RentalApp.Contracts;

namespace RentalApp.Application.Services;

public interface IApiClient
{
    Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default);
    Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> PutAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> PatchAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken = default);
}

public sealed class ApiClientException : Exception
{
    public ApiClientException(
        string message,
        HttpStatusCode statusCode,
        string? correlationId = null)
        : base(message)
    {
        StatusCode = statusCode;
        CorrelationId = correlationId;
    }

    public HttpStatusCode StatusCode { get; }
    public string? CorrelationId { get; }
}

public sealed class ApiClient : IApiClient
{
    private const int MaximumNetworkAttempts = 3;
    private readonly HttpClient _httpClient;
    private readonly ITokenStore _tokenStore;

    // Presentation point: one authenticated HTTP abstraction centralises bearer
    // headers, token refresh, JSON configuration, timeout handling, and API errors.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, ITokenStore tokenStore)
    {
        _httpClient = httpClient;
        _tokenStore = tokenStore;
        _jsonOptions = CreateJsonOptions();
    }

    public Task<T> GetAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        return SendAsync<T>(HttpMethod.Get, path, null, cancellationToken);
    }

    public Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Post, path, request, cancellationToken);
    }

    public Task<TResponse> PutAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Put, path, request, cancellationToken);
    }

    public Task<TResponse> PatchAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<TResponse>(HttpMethod.Patch, path, request, cancellationToken);
    }

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var response = await SendOnceAsync(method, path, body, cancellationToken);
        // A failed access token is refreshed once, then the original request is
        // replayed. Authentication endpoints are excluded to prevent recursion.
        if (response.StatusCode == HttpStatusCode.Unauthorized
            && !path.StartsWith("auth/", StringComparison.OrdinalIgnoreCase)
            && await TryRefreshAsync(cancellationToken))
        {
            response.Dispose();
            response = await SendOnceAsync(method, path, body, cancellationToken);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                ApiError? error = null;
                try
                {
                    error = await response.Content.ReadFromJsonAsync<ApiError>(_jsonOptions, cancellationToken);
                }
                catch (JsonException)
                {
                    // Authentication middleware can return an empty non-success response.
                }

                throw new ApiClientException(
                    error?.Error ?? "The server rejected the request.",
                    response.StatusCode,
                    GetCorrelationId(response));
            }

            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken)
                ?? throw new ApiClientException(
                    "The server returned an empty response.",
                    response.StatusCode,
                    GetCorrelationId(response));
        }
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var tokens = await _tokenStore.GetAsync();
        var correlationId = Guid.NewGuid().ToString("N");
        var canRetryTransientNetworkFailure = method == HttpMethod.Get
            || path.Equals("auth/token", StringComparison.OrdinalIgnoreCase);

        for (var attempt = 1; attempt <= MaximumNetworkAttempts; attempt++)
        {
            // HttpRequestMessage cannot be sent twice, so each retry gets a new
            // message while reusing the same strongly typed request body.
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("X-Correlation-ID", correlationId);
            if (tokens is not null)
            {
                // Tokens come from Android Secure Storage through ITokenStore.
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
            }

            if (body is not null)
            {
                request.Content = JsonContent.Create(body, body.GetType(), options: _jsonOptions);
            }

            try
            {
                return await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (HttpRequestException) when (
                canRetryTransientNetworkFailure && attempt < MaximumNetworkAttempts)
            {
                // A newly booted emulator can briefly close sockets while its
                // virtual network settles. Retry safe GETs and sign-in only.
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ApiClientException(
                    "The server took too long to respond. Check that Docker is running and VS Code is not forwarding port 8080.",
                    HttpStatusCode.RequestTimeout,
                    correlationId);
            }
            catch (HttpRequestException)
            {
                throw new ApiClientException(
                    "Cannot reach the RentalApp API. Check Docker and the network connection.",
                    HttpStatusCode.ServiceUnavailable,
                    correlationId);
            }
        }

        throw new ApiClientException(
            "Cannot reach the RentalApp API after three attempts.",
            HttpStatusCode.ServiceUnavailable,
            correlationId);
    }

    private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
    {
        // Only one concurrent request may rotate the single-use refresh token.
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var tokens = await _tokenStore.GetAsync();
            if (tokens is null)
            {
                return false;
            }

            using var response = await _httpClient.PostAsJsonAsync(
                "auth/refresh",
                new RefreshTokenRequest(tokens.RefreshToken),
                _jsonOptions,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                await _tokenStore.ClearAsync();
                return false;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions, cancellationToken);
            if (auth is null)
            {
                await _tokenStore.ClearAsync();
                return false;
            }

            var storedTokens = new StoredTokens(
                auth.AccessToken,
                auth.RefreshToken,
                auth.ExpiresAtUtc);
            await _tokenStore.SaveAsync(storedTokens);
            return true;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string? GetCorrelationId(HttpResponseMessage response) =>
        response.Headers.TryGetValues("X-Correlation-ID", out var values)
            ? values.FirstOrDefault()
            : null;
}
