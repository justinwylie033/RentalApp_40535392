using System.Net;
using System.Text;
using RentalApp.Application.Services;

namespace RentalApp.Test.Services;

public sealed class ApiClientTests
{
    [Fact]
    public async Task GetAsync_AuthorisedRequest_AddsBearerTokenAndDeserializesResponse()
    {
        string? capturedScheme = null;
        string? capturedToken = null;
        string? capturedCorrelationId = null;
        var handler = new StubHandler(request =>
        {
            capturedScheme = request.Headers.Authorization?.Scheme;
            capturedToken = request.Headers.Authorization?.Parameter;
            capturedCorrelationId = request.Headers.GetValues("X-Correlation-ID").Single();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":42}", Encoding.UTF8, "application/json")
            };
        });
        var tokenStore = new MemoryTokenStore(new StoredTokens("access-token", "refresh-token", DateTimeOffset.UtcNow.AddMinutes(5)));
        var client = new ApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, tokenStore);

        var result = await client.GetAsync<SampleResponse>("sample");

        Assert.Equal(42, result.Value);
        Assert.Equal("Bearer", capturedScheme);
        Assert.Equal("access-token", capturedToken);
        Assert.Equal(32, capturedCorrelationId?.Length);
    }

    [Fact]
    public async Task GetAsync_BadRequest_ThrowsApiMessage()
    {
        var handler = new StubHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"error\":\"Invalid request\"}", Encoding.UTF8, "application/json")
            };
            response.Headers.Add("X-Correlation-ID", "server-reference-123");
            return response;
        });
        var client = new ApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new MemoryTokenStore(null));

        var exception = await Assert.ThrowsAsync<ApiClientException>(() => client.GetAsync<SampleResponse>("sample"));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Equal("Invalid request", exception.Message);
        Assert.Equal("server-reference-123", exception.CorrelationId);
    }

    [Fact]
    public async Task GetAsync_NetworkUnavailable_ThrowsHelpfulMessage()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("Network unavailable"));
        var client = new ApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new MemoryTokenStore(null));

        var exception = await Assert.ThrowsAsync<ApiClientException>(
            () => client.GetAsync<SampleResponse>("sample"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Contains("Cannot reach", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginAsync_TransientSocketFailures_RetriesAndSucceeds()
    {
        var attempts = 0;
        var handler = new StubHandler(_ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new HttpRequestException("Socket closed while emulator network started");
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"value\":42}", Encoding.UTF8, "application/json")
            };
        });
        var client = new ApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") },
            new MemoryTokenStore(null));

        var result = await client.PostAsync<object, SampleResponse>(
            "auth/token",
            new { Email = "user@example.com", Password = "Password123!" });

        Assert.Equal(42, result.Value);
        Assert.Equal(3, attempts);
    }

    private sealed record SampleResponse(int Value);

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
    }

    private sealed class MemoryTokenStore(StoredTokens? tokens) : ITokenStore
    {
        private StoredTokens? _tokens = tokens;

        public Task<StoredTokens?> GetAsync() => Task.FromResult(_tokens);

        public Task SaveAsync(StoredTokens newTokens)
        {
            _tokens = newTokens;
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _tokens = null;
            return Task.CompletedTask;
        }
    }
}
