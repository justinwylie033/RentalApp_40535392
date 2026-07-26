using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using RentalApp.Api.Services;
using RentalApp.Database.Models;

namespace RentalApp.Test.Services;

public sealed class TokenServiceTests
{
    private readonly TokenService _service = new(Options.Create(new JwtOptions
    {
        Issuer = "RentalApp.Tests",
        Audience = "RentalApp.TestClient",
        Secret = "a-test-secret-that-is-definitely-longer-than-32-characters",
        AccessTokenMinutes = 10,
        RefreshTokenDays = 1
    }));

    [Fact]
    public void CreateAccessToken_User_CreatesExpectedIdentityClaims()
    {
        var user = new User
        {
            DisplayName = "Mike Tester",
            Email = "mike@test.local",
            PasswordHash = "hash"
        };

        var (token, expiresAtUtc) = _service.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, claim => claim.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Value == user.Email);
        Assert.True(expiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateRefreshToken_RepeatedCalls_ProducesUniqueHashedTokens()
    {
        var first = _service.CreateRefreshToken();
        var second = _service.CreateRefreshToken();

        Assert.NotEqual(first, second);
        Assert.Equal(_service.HashRefreshToken(first), _service.HashRefreshToken(first));
        Assert.NotEqual(_service.HashRefreshToken(first), _service.HashRefreshToken(second));
    }
}
