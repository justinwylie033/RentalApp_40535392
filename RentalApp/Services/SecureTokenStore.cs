using RentalApp.Application.Services;

namespace RentalApp.Services;

public sealed class SecureTokenStore : ITokenStore
{
    private const string AccessTokenKey = "access_token";
    private const string RefreshTokenKey = "refresh_token";
    private const string ExpiryKey = "token_expiry";

    public async Task<StoredTokens?> GetAsync()
    {
        var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
        var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        var expiryText = await SecureStorage.Default.GetAsync(ExpiryKey);
        return accessToken is null
            || refreshToken is null
            || !DateTimeOffset.TryParse(expiryText, out var expiry)
            ? null
            : new StoredTokens(accessToken, refreshToken, expiry);
    }

    public async Task SaveAsync(StoredTokens tokens)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, tokens.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, tokens.RefreshToken);
        await SecureStorage.Default.SetAsync(ExpiryKey, tokens.ExpiresAtUtc.ToString("O"));
    }

    public Task ClearAsync()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(ExpiryKey);
        return Task.CompletedTask;
    }
}
