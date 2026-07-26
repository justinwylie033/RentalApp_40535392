using Moq;
using NetTopologySuite.Geometries;
using RentalApp.Api.Services;
using RentalApp.Contracts;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;

namespace RentalApp.Test.Services;

public sealed class LocationServiceTests
{
    [Fact]
    public async Task FindNearbyAsync_ValidSearch_MapsDistanceToKilometres()
    {
        var owner = new User { DisplayName = "Owner", Email = "owner@test.local", PasswordHash = "hash" };
        var item = new Item
        {
            Owner = owner,
            Title = "Tent",
            Description = "A waterproof camping tent.",
            DailyRate = 12m,
            Category = ItemCategory.Camping,
            Location = new Point(-3.18, 55.95) { SRID = 4326 }
        };
        var repository = new Mock<IItemRepository>();
        repository.Setup(candidate => candidate.GetNearbyAsync(55.95, -3.18, 5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Item Item, double DistanceMetres)> { (item, 1_250d) });
        var service = new LocationService(repository.Object);

        var result = await service.FindNearbyAsync(55.95, -3.18, 5, null);

        Assert.Single(result);
        Assert.Equal(1.25, result[0].DistanceKm);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(101)]
    public async Task FindNearbyAsync_InvalidRadius_RejectsSearch(double radius)
    {
        var service = new LocationService(Mock.Of<IItemRepository>());

        await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.FindNearbyAsync(55.95, -3.18, radius, null));
    }
}
