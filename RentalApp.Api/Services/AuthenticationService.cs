using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RentalApp.Contracts;
using RentalApp.Database.Data;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;

namespace RentalApp.Api.Services;

public interface IAuthenticationService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService(
    AppDbContext context,
    ITokenService tokenService,
    IReviewRepository reviewRepository,
    IOptions<JwtOptions> options) : IAuthenticationService
{
    // Presentation point: passwords are one-way hashed and refresh tokens are stored
    // only as hashes, so database contents cannot be replayed as bearer credentials.
    private readonly PasswordHasher<User> _passwordHasher = new();

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        ValidateRegistration(request.DisplayName, email, request.Password);

        if (await context.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            throw new BusinessRuleException("An account already exists for that email address.");
        }

        var user = new User
        {
            DisplayName = request.DisplayName.Trim(),
            Email = email,
            PasswordHash = string.Empty
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        context.Users.Add(user);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await context.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken)
            ?? throw new BusinessRuleException("Email or password is incorrect.");

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new BusinessRuleException("Email or password is incorrect.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await context.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken)
            ?? throw new BusinessRuleException("The refresh token is invalid.");

        if (storedToken.RevokedAtUtc is not null || storedToken.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new BusinessRuleException("The refresh token has expired or was revoked.");
        }

        storedToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        // Rotation makes each refresh token single-use and limits replay attacks.
        return await IssueTokensAsync(storedToken.User, cancellationToken);
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");
        var (average, count) = await reviewRepository.GetUserRatingAsync(userId, cancellationToken);
        return new UserProfileDto(user.Id, user.DisplayName, user.Email, average, count);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (accessToken, expiresAtUtc) = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.CreateRefreshToken();
        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshToken),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(options.Value.RefreshTokenDays)
        });
        await context.SaveChangesAsync(cancellationToken);

        var (average, count) = await reviewRepository.GetUserRatingAsync(user.Id, cancellationToken);
        var profile = new UserProfileDto(user.Id, user.DisplayName, user.Email, average, count);
        return new AuthResponse(accessToken, refreshToken, expiresAtUtc, profile);
    }

    private static void ValidateRegistration(string displayName, string email, string password)
    {
        if (displayName.Trim().Length is < 2 or > 80)
        {
            throw new BusinessRuleException("Display name must contain between 2 and 80 characters.");
        }

        if (!email.Contains('@') || email.Length > 254)
        {
            throw new BusinessRuleException("Enter a valid email address.");
        }

        if (password.Length < 8 || !password.Any(char.IsLetter) || !password.Any(char.IsDigit))
        {
            throw new BusinessRuleException("Password must contain at least eight characters, a letter, and a number.");
        }
    }
}
