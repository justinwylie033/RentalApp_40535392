namespace RentalApp.Application.Services;

public sealed record StoredTokens(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc);

/// <summary>Stores authentication tokens behind a platform-independent boundary.</summary>
public interface ITokenStore
{
    /// <summary>Returns the current token pair, or null when signed out.</summary>
    Task<StoredTokens?> GetAsync();
    /// <summary>Persists the current token pair securely.</summary>
    Task SaveAsync(StoredTokens tokens);
    /// <summary>Removes all locally stored authentication tokens.</summary>
    Task ClearAsync();
}
