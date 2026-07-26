using RentalApp.Contracts;

namespace RentalApp.Application.Services;

public interface IAuthenticationService
{
    Task<UserProfileDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<UserProfileDto> RegisterAsync(string displayName, string email, string password, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync();
}

public sealed class AuthenticationService(IApiClient api, ITokenStore tokenStore) : IAuthenticationService
{
    public async Task<UserProfileDto> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var response = await api.PostAsync<LoginRequest, AuthResponse>(
            "auth/token",
            new LoginRequest(email, password),
            cancellationToken);
        await SaveTokensAsync(response);
        return response.User;
    }

    public async Task<UserProfileDto> RegisterAsync(
        string displayName,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var response = await api.PostAsync<RegisterRequest, AuthResponse>(
            "auth/register",
            new RegisterRequest(displayName, email, password),
            cancellationToken);
        await SaveTokensAsync(response);
        return response.User;
    }

    public Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<UserProfileDto>("auth/me", cancellationToken);

    public Task LogoutAsync() => tokenStore.ClearAsync();

    private Task SaveTokensAsync(AuthResponse response) =>
        tokenStore.SaveAsync(new StoredTokens(response.AccessToken, response.RefreshToken, response.ExpiresAtUtc));
}
