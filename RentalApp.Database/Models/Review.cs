namespace RentalApp.Database.Models;

public sealed class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RentalId { get; set; }
    public Guid ItemId { get; set; }
    public Guid ReviewerId { get; set; }
    public int Rating { get; set; }
    public required string Comment { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Rental Rental { get; set; } = null!;
    public Item Item { get; set; } = null!;
    public User Reviewer { get; set; } = null!;
}
