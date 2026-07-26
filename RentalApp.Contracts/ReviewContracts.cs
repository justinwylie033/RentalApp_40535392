namespace RentalApp.Contracts;

public sealed record CreateReviewRequest(Guid RentalId, int Rating, string Comment);

public sealed record ReviewDto(
    Guid Id,
    Guid RentalId,
    Guid ItemId,
    Guid ReviewerId,
    string ReviewerName,
    int Rating,
    string Comment,
    DateTimeOffset CreatedAtUtc);
