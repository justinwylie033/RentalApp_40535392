using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using RentalApp.Contracts;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Api;

[Collection(DatabaseCollection.Name)]
public sealed class RentalApiTests(DatabaseFixture database) : IAsyncLifetime
{
    // Presentation point: WebApplicationFactory exercises the real HTTP pipeline,
    // JWT authentication, services, EF Core migrations, and PostGIS database.
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private RentalApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        // Every test uses the isolated rentalapp_test database created by the fixture.
        _factory = new RentalApiFactory(database.ConnectionString);
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task AuthenticationEndpoints_RegisterLoginRefreshAndProfile_CompleteRoundTrip()
    {
        var email = $"coverage-{Guid.NewGuid():N}@test.local";
        var registerResponse = await _client.PostAsJsonAsync(
            "/auth/register",
            new RegisterRequest("Coverage User", email.ToUpperInvariant(), "Testing123!"),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await ReadAsync<AuthResponse>(registerResponse);
        Assert.Equal(email, registered.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(registered.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(registered.RefreshToken));

        var loginResponse = await _client.PostAsJsonAsync(
            "/auth/token",
            new LoginRequest(email, "Testing123!"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loggedIn = await ReadAsync<AuthResponse>(loginResponse);

        using var profileResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            "/auth/me",
            loggedIn.AccessToken);
        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        var profile = await ReadAsync<UserProfileDto>(profileResponse);
        Assert.Equal(registered.User.Id, profile.Id);

        var refreshResponse = await _client.PostAsJsonAsync(
            "/auth/refresh",
            new RefreshTokenRequest(loggedIn.RefreshToken),
            JsonOptions);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await ReadAsync<AuthResponse>(refreshResponse);
        Assert.NotEqual(loggedIn.RefreshToken, refreshed.RefreshToken);

        var reusedRefreshResponse = await _client.PostAsJsonAsync(
            "/auth/refresh",
            new RefreshTokenRequest(loggedIn.RefreshToken),
            JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, reusedRefreshResponse.StatusCode);

        var invalidLoginResponse = await _client.PostAsJsonAsync(
            "/auth/token",
            new LoginRequest(email, "incorrect-password"),
            JsonOptions);
        Assert.Equal(HttpStatusCode.BadRequest, invalidLoginResponse.StatusCode);
    }

    [Fact]
    public async Task ItemEndpoints_OwnerCrudNearbyAndPermissions_EnforceRules()
    {
        var owner = await LoginAsync("sarah@example.com", "Rental123!");
        var borrower = await LoginAsync("mike@example.com", "Rental123!");

        using var unauthenticated = await _client.GetAsync("/items/");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        using var browseResponse = await SendAuthorizedAsync(HttpMethod.Get, "/items/", owner.AccessToken);
        Assert.Equal(HttpStatusCode.OK, browseResponse.StatusCode);
        Assert.NotEmpty((await ReadAsync<PagedResult<ItemSummaryDto>>(browseResponse)).Items);

        var create = new CreateItemRequest(
            "Coverage pressure washer",
            "A pressure washer created by the API integration test.",
            22.50m,
            ItemCategory.Tools,
            55.9533,
            -3.1883,
            "10 Princes Street, Edinburgh, EH2 2ER");
        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/items/",
            owner.AccessToken,
            create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await ReadAsync<ItemDetailDto>(createResponse);

        using var detailResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/items/{created.Id}",
            borrower.AccessToken);
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = await ReadAsync<ItemDetailDto>(detailResponse);
        Assert.Equal(create.Title, detail.Title);
        Assert.Equal(create.Address, detail.Address);

        var update = new UpdateItemRequest(
            "Updated pressure washer",
            "The updated description remains long enough for validation.",
            25m,
            ItemCategory.Tools,
            55.9534,
            -3.1884,
            true,
            "12 Princes Street, Edinburgh, EH2 2ER");
        using var updateResponse = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/items/{created.Id}",
            owner.AccessToken,
            update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(25m, (await ReadAsync<ItemDetailDto>(updateResponse)).DailyRate);

        using var forbiddenUpdate = await SendAuthorizedAsync(
            HttpMethod.Put,
            $"/items/{created.Id}",
            borrower.AccessToken,
            update);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdate.StatusCode);

        using var nearbyResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            "/items/nearby?latitude=55.9533&longitude=-3.1883&radiusKm=2&category=Tools",
            borrower.AccessToken);
        Assert.Equal(HttpStatusCode.OK, nearbyResponse.StatusCode);
        var nearby = await ReadAsync<List<ItemSummaryDto>>(nearbyResponse);
        Assert.Contains(nearby, item => item.Id == created.Id && item.DistanceKm is not null);

        using var invalidRadiusResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            "/items/nearby?latitude=55.9533&longitude=-3.1883&radiusKm=0",
            borrower.AccessToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidRadiusResponse.StatusCode);

        using var missingResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/items/{Guid.NewGuid()}",
            owner.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
    }

    [Fact]
    public async Task RentalAndReviewEndpoints_FullRoleAwareWorkflow_Succeeds()
    {
        var owner = await LoginAsync("sarah@example.com", "Rental123!");
        var borrower = await LoginAsync("mike@example.com", "Rental123!");
        using var itemResponse = await SendAuthorizedAsync(HttpMethod.Get, "/items/", borrower.AccessToken);
        var item = (await ReadAsync<PagedResult<ItemSummaryDto>>(itemResponse)).Items
            .Single(candidate => candidate.Title == "18V Cordless Drill");
        var start = DateTimeOffset.UtcNow.Date.AddDays(20);

        using var requestResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/rentals/",
            borrower.AccessToken,
            new CreateRentalRequest(item.Id, start, start.AddDays(2)));
        Assert.Equal(HttpStatusCode.Created, requestResponse.StatusCode);
        var rental = await ReadAsync<RentalSummaryDto>(requestResponse);
        Assert.Equal(RentalStatus.Requested, rental.Status);
        Assert.Equal(item.DailyRate * 3, rental.TotalPrice);

        using var incomingResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            "/rentals/incoming",
            owner.AccessToken);
        Assert.Contains(await ReadAsync<List<RentalSummaryDto>>(incomingResponse), value => value.Id == rental.Id);

        using var outgoingResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            "/rentals/outgoing",
            borrower.AccessToken);
        Assert.Contains(await ReadAsync<List<RentalSummaryDto>>(outgoingResponse), value => value.Id == rental.Id);

        await AssertTransitionAsync(owner.AccessToken, rental.Id, RentalStatus.Approved);
        await AssertTransitionAsync(owner.AccessToken, rental.Id, RentalStatus.OutForRent);
        await AssertTransitionAsync(borrower.AccessToken, rental.Id, RentalStatus.Returned);
        await AssertTransitionAsync(owner.AccessToken, rental.Id, RentalStatus.Completed);

        using var reviewResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/reviews/",
            borrower.AccessToken,
            new CreateReviewRequest(rental.Id, 5, "Excellent integration test item."));
        Assert.Equal(HttpStatusCode.Created, reviewResponse.StatusCode);
        var review = await ReadAsync<ReviewDto>(reviewResponse);
        Assert.Equal(item.Id, review.ItemId);

        using var listReviewsResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/reviews/items/{item.Id}",
            owner.AccessToken);
        Assert.Contains(await ReadAsync<List<ReviewDto>>(listReviewsResponse), value => value.Id == review.Id);

        // The summary DTO drives the Browse and Near me rating labels, so it must
        // expose the same verified review count as the item-details response.
        using var updatedItemsResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            "/items/",
            borrower.AccessToken);
        var reviewedItem = (await ReadAsync<PagedResult<ItemSummaryDto>>(updatedItemsResponse)).Items
            .Single(candidate => candidate.Id == item.Id);
        Assert.Equal(5, reviewedItem.AverageRating);
        Assert.Equal(1, reviewedItem.ReviewCount);

        using var duplicateReviewResponse = await SendAuthorizedAsync(
            HttpMethod.Post,
            "/reviews/",
            borrower.AccessToken,
            new CreateReviewRequest(rental.Id, 4, "A duplicate review is rejected."));
        Assert.Equal(HttpStatusCode.BadRequest, duplicateReviewResponse.StatusCode);
    }

    private async Task<AuthResponse> LoginAsync(string email, string password)
    {
        using var response = await _client.PostAsJsonAsync(
            "/auth/token",
            new LoginRequest(email, password),
            JsonOptions);
        response.EnsureSuccessStatusCode();
        return await ReadAsync<AuthResponse>(response);
    }

    private async Task AssertTransitionAsync(string token, Guid rentalId, RentalStatus status)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Patch,
            $"/rentals/{rentalId}/status",
            token,
            new UpdateRentalStatusRequest(status));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(status, (await ReadAsync<RentalSummaryDto>(response)).Status);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync<T>(
        HttpMethod method,
        string path,
        string accessToken,
        T? body = default)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return await _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string path,
        string accessToken) =>
        SendAuthorizedAsync<object>(method, path, accessToken);

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<T>(JsonOptions)
        ?? throw new InvalidOperationException("The API returned an empty JSON body.");

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
