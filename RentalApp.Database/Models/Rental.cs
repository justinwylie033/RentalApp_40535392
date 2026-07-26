using RentalApp.Contracts;

namespace RentalApp.Database.Models;

public sealed class Rental
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ItemId { get; set; }
    public Guid BorrowerId { get; set; }
    public DateTimeOffset StartDateUtc { get; set; }
    public DateTimeOffset EndDateUtc { get; set; }
    public decimal TotalPrice { get; set; }
    public RentalStatus Status { get; set; } = RentalStatus.Requested;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Item Item { get; set; } = null!;
    public User Borrower { get; set; } = null!;
    public Review? Review { get; set; }
}
