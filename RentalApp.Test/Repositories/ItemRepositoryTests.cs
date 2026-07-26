using NetTopologySuite.Geometries;
using RentalApp.Contracts;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;
using RentalApp.Test.Fixtures;

namespace RentalApp.Test.Repositories;

[Collection(DatabaseCollection.Name)]
public sealed class ItemRepositoryTests(DatabaseFixture fixture)
{
    // Presentation point: this is a true PostgreSQL/PostGIS integration test, not an
    // in-memory substitute that could hide provider-specific spatial behaviour.
    [Fact]
    public async Task GetNearbyAsync_ItemsInsideAndOutsideRadius_ReturnsOnlyNearbyItem()
    {
        await using var context = fixture.CreateContext();
        var owner = new User { DisplayName = "Spatial Owner", Email = $"{Guid.NewGuid()}@test.local", PasswordHash = "hash" };
        var nearby = CreateItem(owner, "Nearby drill", -3.1883, 55.9533);
        var distant = CreateItem(owner, "Distant drill", -3.0, 55.8);
        context.AddRange(owner, nearby, distant);
        await context.SaveChangesAsync();
        var repository = new ItemRepository(context);

        // The 2 km radius should include the Edinburgh-centre item only.
        var results = await repository.GetNearbyAsync(55.9533, -3.1883, 2, ItemCategory.Tools);

        Assert.Contains(results, result => result.Item.Id == nearby.Id);
        Assert.DoesNotContain(results, result => result.Item.Id == distant.Id);
        Assert.All(results, result => Assert.InRange(result.DistanceMetres, 0, 2_000));
    }

    private static Item CreateItem(User owner, string title, double longitude, double latitude) => new()
    {
        Owner = owner,
        Title = title,
        Description = "Repository integration test item.",
        DailyRate = 5m,
        Category = ItemCategory.Tools,
        Location = new Point(longitude, latitude) { SRID = 4326 }
    };
}
