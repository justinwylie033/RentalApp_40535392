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

public enum RentalStatus
{
    Requested,
    Approved,
    Rejected,
    OutForRent,
    Overdue,
    Returned,
    Completed
}
