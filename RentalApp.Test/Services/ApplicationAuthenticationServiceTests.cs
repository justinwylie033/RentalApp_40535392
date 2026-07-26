using Moq;
using RentalApp.Application.Services;
using RentalApp.Contracts;
using ClientAuthenticationService = RentalApp.Application.Services.AuthenticationService;

namespace RentalApp.Test.Services;

public sealed class ApplicationAuthenticationServiceTests
{
    [Fact]
    public async Task LoginAsync_SuccessfulResponse_SavesTokensAndReturnsProfile()
    {
        var profile = CreateProfile();
        var response = CreateResponse(profile);
        var api = new Mock<IApiClient>();
        api.Setup(client => client.PostAsync<LoginRequest, AuthResponse>(
                "auth/token",
                It.Is<LoginRequest>(request => request.Email == profile.Email && request.Password == "Password123"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var tokenStore = CreateTokenStore();
        var service = new ClientAuthenticationService(api.Object, tokenStore.Object);

        var result = await service.LoginAsync(profile.Email, "Password123");

        Assert.Equal(profile, result);
        tokenStore.Verify(store => store.SaveAsync(It.Is<StoredTokens>(tokens =>
            tokens.AccessToken == response.AccessToken
            && tokens.RefreshToken == response.RefreshToken
            && tokens.ExpiresAtUtc == response.ExpiresAtUtc)), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_SuccessfulResponse_SavesTokensAndReturnsProfile()
    {
        var profile = CreateProfile();
        var response = CreateResponse(profile);
        var api = new Mock<IApiClient>();
        api.Setup(client => client.PostAsync<RegisterRequest, AuthResponse>(
                "auth/register",
                It.Is<RegisterRequest>(request => request.DisplayName == profile.DisplayName),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
        var tokenStore = CreateTokenStore();
        var service = new ClientAuthenticationService(api.Object, tokenStore.Object);

        var result = await service.RegisterAsync(profile.DisplayName, profile.Email, "Password123");

        Assert.Equal(profile, result);
        tokenStore.Verify(store => store.SaveAsync(It.IsAny<StoredTokens>()), Times.Once);
    }

    [Fact]
    public async Task GetProfileAsync_AuthenticatedClient_ReturnsApiProfile()
    {
        var profile = CreateProfile();
        var api = new Mock<IApiClient>();
        api.Setup(client => client.GetAsync<UserProfileDto>("auth/me", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var service = new ClientAuthenticationService(api.Object, Mock.Of<ITokenStore>());

        var result = await service.GetProfileAsync();

        Assert.Equal(profile, result);
    }

    [Fact]
    public async Task LogoutAsync_ExistingTokens_ClearsTokenStore()
    {
        var tokenStore = CreateTokenStore();
        var service = new ClientAuthenticationService(Mock.Of<IApiClient>(), tokenStore.Object);

        await service.LogoutAsync();

        tokenStore.Verify(store => store.ClearAsync(), Times.Once);
    }

    private static Mock<ITokenStore> CreateTokenStore()
    {
        var store = new Mock<ITokenStore>();
        store.Setup(value => value.SaveAsync(It.IsAny<StoredTokens>())).Returns(Task.CompletedTask);
        store.Setup(value => value.ClearAsync()).Returns(Task.CompletedTask);
        return store;
    }

    private static UserProfileDto CreateProfile() =>
        new(Guid.NewGuid(), "Coverage User", "coverage@test.local", 4.5, 2);

    private static AuthResponse CreateResponse(UserProfileDto profile) =>
        new("access-token", "refresh-token", DateTimeOffset.UtcNow.AddMinutes(30), profile);
}
