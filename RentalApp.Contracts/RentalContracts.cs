namespace RentalApp.Contracts;

public sealed record CreateRentalRequest(
    Guid ItemId,
    DateTimeOffset StartDateUtc,
    DateTimeOffset EndDateUtc);

public sealed record UpdateRentalStatusRequest(RentalStatus Status);

public sealed record RentalSummaryDto(
    Guid Id,
    Guid ItemId,
    string ItemTitle,
    Guid OwnerId,
    string OwnerName,
    Guid BorrowerId,
    string BorrowerName,
    DateTimeOffset StartDateUtc,
    DateTimeOffset EndDateUtc,
    decimal TotalPrice,
    RentalStatus Status,
    DateTimeOffset CreatedAtUtc);
