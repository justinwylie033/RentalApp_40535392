using RentalApp.Api.Services;

namespace RentalApp.Test.Services;

public sealed class CatalogueRequestValidatorTests
{
    [Theory]
    [InlineData(0, 20, "Page")]
    [InlineData(1, 0, "Page size")]
    [InlineData(1, 51, "Page size")]
    public void ValidateQuery_InvalidPaging_ThrowsClearRule(
        int page,
        int pageSize,
        string expectedMessage)
    {
        var exception = Assert.Throws<BusinessRuleException>(
            () => CatalogueRequestValidator.ValidateQuery(null, page, pageSize));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("ab", "A valid long description.", 5, "10 Test Street", 55.95, -3.18, "Title")]
    [InlineData("Valid title", "short", 5, "10 Test Street", 55.95, -3.18, "Description")]
    [InlineData("Valid title", "A valid long description.", 0, "10 Test Street", 55.95, -3.18, "rate")]
    [InlineData("Valid title", "A valid long description.", 5, "", 55.95, -3.18, "address")]
    public void ValidateItem_InvalidInput_ThrowsClearRule(
        string title,
        string description,
        decimal rate,
        string address,
        double latitude,
        double longitude,
        string expectedMessage)
    {
        var exception = Assert.Throws<BusinessRuleException>(() =>
            CatalogueRequestValidator.ValidateItem(
                title,
                description,
                rate,
                address,
                latitude,
                longitude));

        Assert.Contains(expectedMessage, exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
