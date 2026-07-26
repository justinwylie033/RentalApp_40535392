namespace RentalApp.Contracts;

public sealed record RegisterRequest(string DisplayName, string Email, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc,
    UserProfileDto User);

public sealed record UserProfileDto(
    Guid Id,
    string DisplayName,
    string Email,
    double AverageRating,
    int ReviewCount);
