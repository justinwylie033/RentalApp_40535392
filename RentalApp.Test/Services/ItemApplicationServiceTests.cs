using NetTopologySuite.Geometries;
using RentalApp.Api.Services;
using RentalApp.Contracts;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Services;

public sealed class ItemApplicationServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidListing_TrimsAndPersistsItem()
    {
        await using var context = TestContextFactory.Create();
        var owner = new User
        {
            DisplayName = "Owner",
            Email = "owner@test.local",
            PasswordHash = "hash"
        };
        context.Users.Add(owner);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.CreateAsync(owner.Id, new CreateItemRequest(
            "  Cordless drill  ",
            "  Includes a charger and two batteries.  ",
            9.5m,
            ItemCategory.Tools,
            55.9533,
            -3.1883,
            "  10 Princes Street, Edinburgh, EH2 2ER  "));

        Assert.Equal("Cordless drill", result.Title);
        Assert.Equal(9.5m, result.DailyRate);
        Assert.Equal("10 Princes Street, Edinburgh, EH2 2ER", result.Address);
        Assert.Single(context.Items);
        Assert.Equal(-3.1883, context.Items.Single().Location.X);
    }

    [Fact]
    public async Task UpdateAsync_DifferentUser_RejectsOwnerOnlyChange()
    {
        await using var context = TestContextFactory.Create();
        var owner = CreateUser("owner@test.local");
        var otherUser = CreateUser("other@test.local");
        var item = CreateItem(owner);
        context.AddRange(owner, otherUser, item);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.UpdateAsync(
            otherUser.Id,
            item.Id,
            new UpdateItemRequest(
                item.Title,
                item.Description,
                item.DailyRate,
                item.Category,
                item.Location.Y,
                item.Location.X,
                false,
                item.Address)));

        Assert.Contains("owner", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(item.IsAvailable);
    }

    [Theory]
    [InlineData(91, -3.18)]
    [InlineData(55.95, 181)]
    public async Task CreateAsync_InvalidCoordinates_RejectsListing(double latitude, double longitude)
    {
        await using var context = TestContextFactory.Create();
        var service = CreateService(context);

        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(
            Guid.NewGuid(),
            new CreateItemRequest(
                "Camping tent",
                "A complete waterproof camping tent.",
                12m,
                ItemCategory.Camping,
                latitude,
                longitude,
                "10 Test Street, Edinburgh, EH1 1AA")));
    }

    [Fact]
    public async Task GetAllAsync_SearchAndPriceSort_ReturnsMatchingItemsInOrder()
    {
        await using var context = TestContextFactory.Create();
        var owner = CreateUser("catalogue@test.local");
        var expensive = CreateItem(owner);
        expensive.Title = "Premium drill";
        expensive.DailyRate = 20m;
        var affordable = CreateItem(owner);
        affordable.Title = "Compact drill";
        affordable.DailyRate = 5m;
        var unrelated = CreateItem(owner);
        unrelated.Title = "Camping stove";
        context.AddRange(owner, expensive, affordable, unrelated);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var results = await service.GetAllAsync(
            ItemCategory.Tools,
            "drill",
            ItemSortOrder.PriceLowToHigh,
            1,
            20);

        Assert.Equal([affordable.Id, expensive.Id], results.Items.Select(item => item.Id));
        Assert.Equal(2, results.TotalCount);
    }

    private static ItemApplicationService CreateService(RentalApp.Database.Data.AppDbContext context) =>
        new(new ItemRepository(context), new UnitOfWork(context));

    private static User CreateUser(string email) => new()
    {
        DisplayName = "Test user",
        Email = email,
        PasswordHash = "hash"
    };

    private static Item CreateItem(User owner) => new()
    {
        Owner = owner,
        Title = "Cordless drill",
        Description = "Includes two batteries and a charger.",
        DailyRate = 9m,
        Category = ItemCategory.Tools,
        Location = new Point(-3.1883, 55.9533) { SRID = 4326 }
    };
}
