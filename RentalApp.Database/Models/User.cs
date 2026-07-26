namespace RentalApp.Database.Models;

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string DisplayName { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Item> Items { get; set; } = [];
    public ICollection<Rental> Rentals { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
