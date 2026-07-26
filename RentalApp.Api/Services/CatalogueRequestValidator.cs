namespace RentalApp.Api.Services;

/// <summary>
/// Centralises marketplace input rules so create, edit and catalogue operations
/// produce the same clear validation behaviour.
/// </summary>
public static class CatalogueRequestValidator
{
    public static void ValidateQuery(string? search, int page, int pageSize)
    {
        if (search?.Trim().Length > 100)
        {
            throw new BusinessRuleException("Search text cannot exceed 100 characters.");
        }

        if (page < 1)
        {
            throw new BusinessRuleException("Page must be at least 1.");
        }

        if (pageSize is < 1 or > 50)
        {
            throw new BusinessRuleException("Page size must be between 1 and 50.");
        }
    }

    public static void ValidateItem(
        string title,
        string description,
        decimal rate,
        string address,
        double latitude,
        double longitude)
    {
        if (title.Trim().Length is < 3 or > 120)
        {
            throw new BusinessRuleException("Title must contain between 3 and 120 characters.");
        }

        if (description.Trim().Length is < 10 or > 1_500)
        {
            throw new BusinessRuleException("Description must contain between 10 and 1,500 characters.");
        }

        if (rate is <= 0 or > 10_000)
        {
            throw new BusinessRuleException("Daily rate must be greater than zero and no more than £10,000.");
        }

        if (string.IsNullOrWhiteSpace(address) || address.Trim().Length is < 5 or > 250)
        {
            throw new BusinessRuleException("Enter a collection address between 5 and 250 characters.");
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new BusinessRuleException("Location coordinates are outside the valid range.");
        }
    }
}
