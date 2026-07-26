namespace RentalApp.Contracts;

public enum ItemCategory
{
    Tools,
    Camping,
    Electronics,
    Games,
    Sports,
    Household,
    Other
}

/// <summary>Supported server-side ordering for the marketplace catalogue.</summary>
public enum ItemSortOrder
{
    Newest,
    PriceLowToHigh,
    PriceHighToLow,
    RatingHighToLow
}

public enum RentalStatus
{
    Requested,
    Approved,
    Rejected,
    Cancelled,
    OutForRent,
    Overdue,
    Returned,
    Completed
}
