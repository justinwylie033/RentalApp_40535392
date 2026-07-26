using NetTopologySuite.Geometries;
using RentalApp.Contracts;

namespace RentalApp.Database.Models;

public sealed class Item
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public decimal DailyRate { get; set; }
    public ItemCategory Category { get; set; }
    public string Address { get; set; } = "Location not specified";
    public required Point Location { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public User Owner { get; set; } = null!;
    public ICollection<Rental> Rentals { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}
