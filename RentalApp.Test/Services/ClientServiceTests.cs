using Moq;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Test.Services;

public sealed class ClientServiceTests
{
    [Fact]
    public async Task ItemService_AllOperations_UseExpectedApiRoutes()
    {
        var itemId = Guid.NewGuid();
        var detail = CreateItemDetail(itemId);
        IReadOnlyList<ItemSummaryDto> itemSummaries = [CreateItemSummary(itemId)];
        var items = new PagedResult<ItemSummaryDto>(itemSummaries, 1, 20, 1);
        var api = new Mock<IApiClient>();
        api.Setup(client => client.GetAsync<PagedResult<ItemSummaryDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);
        api.Setup(client => client.GetAsync<IReadOnlyList<ItemSummaryDto>>(
                "items/mine", It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemSummaries);
        api.Setup(client => client.GetAsync<IReadOnlyList<ItemSummaryDto>>(
                "items/nearby?latitude=55.95&longitude=-3.18&radiusKm=5&category=Tools",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(itemSummaries);
        api.Setup(client => client.GetAsync<ItemDetailDto>($"items/{itemId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);
        api.Setup(client => client.PostAsync<CreateItemRequest, ItemDetailDto>(
                "items/", It.IsAny<CreateItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);
        api.Setup(client => client.PutAsync<UpdateItemRequest, ItemDetailDto>(
                $"items/{itemId}", It.IsAny<UpdateItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detail);
        var service = new ItemService(api.Object);
        var create = new CreateItemRequest(
            "Drill", "A useful test drill.", 8m, ItemCategory.Tools,
            55.95, -3.18, "10 Test Street, Edinburgh, EH1 1AA");
        var update = new UpdateItemRequest(
            "Drill", "An updated test drill.", 9m, ItemCategory.Tools,
            55.95, -3.18, true, "12 Test Street, Edinburgh, EH1 1AA");

        Assert.Single((await service.GetAllAsync()).Items);
        Assert.Single((await service.GetAllAsync(ItemCategory.Tools)).Items);
        Assert.Single(await service.GetMineAsync());
        Assert.Single(await service.FindNearbyAsync(55.95, -3.18, 5, ItemCategory.Tools));
        Assert.Equal(detail, await service.GetAsync(itemId));
        Assert.Equal(detail, await service.CreateAsync(create));
        Assert.Equal(detail, await service.UpdateAsync(itemId, update));

        api.Verify(client => client.GetAsync<PagedResult<ItemSummaryDto>>(
            "items/?sort=Newest&page=1&pageSize=20", It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(client => client.GetAsync<PagedResult<ItemSummaryDto>>(
            "items/?category=Tools&sort=Newest&page=1&pageSize=20", It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(client => client.GetAsync<IReadOnlyList<ItemSummaryDto>>(
            "items/mine", It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(client => client.GetAsync<IReadOnlyList<ItemSummaryDto>>(
            "items/nearby?latitude=55.95&longitude=-3.18&radiusKm=5&category=Tools",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RentalService_AllOperations_UseExpectedApiRoutes()
    {
        var rental = CreateRental();
        IReadOnlyList<RentalSummaryDto> rentals = [rental];
        var api = new Mock<IApiClient>();
        api.Setup(client => client.PostAsync<CreateRentalRequest, RentalSummaryDto>(
                "rentals/", It.IsAny<CreateRentalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rental);
        api.Setup(client => client.GetAsync<IReadOnlyList<RentalSummaryDto>>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rentals);
        api.Setup(client => client.GetAsync<IReadOnlyList<UnavailableDateRangeDto>>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        api.Setup(client => client.PatchAsync<UpdateRentalStatusRequest, RentalSummaryDto>(
                $"rentals/{rental.Id}/status",
                It.Is<UpdateRentalStatusRequest>(request => request.Status == RentalStatus.Approved),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rental with { Status = RentalStatus.Approved });
        var service = new RentalService(api.Object);
        var request = new CreateRentalRequest(rental.ItemId, rental.StartDateUtc, rental.EndDateUtc);

        Assert.Equal(rental, await service.RequestAsync(request));
        Assert.Single(await service.GetIncomingAsync());
        Assert.Single(await service.GetOutgoingAsync());
        Assert.Empty(await service.GetUnavailableDatesAsync(
            rental.ItemId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMonths(1)));
        Assert.Equal(RentalStatus.Approved, (await service.UpdateStatusAsync(rental.Id, RentalStatus.Approved)).Status);

        api.Verify(client => client.GetAsync<IReadOnlyList<RentalSummaryDto>>(
            "rentals/incoming", It.IsAny<CancellationToken>()), Times.Once);
        api.Verify(client => client.GetAsync<IReadOnlyList<RentalSummaryDto>>(
            "rentals/outgoing", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewService_ListAndCreate_UseExpectedApiRoutes()
    {
        var review = new ReviewDto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Reviewer", 5, "Excellent item.", DateTimeOffset.UtcNow);
        IReadOnlyList<ReviewDto> reviews = [review];
        var api = new Mock<IApiClient>();
        api.Setup(client => client.GetAsync<IReadOnlyList<ReviewDto>>(
                $"reviews/items/{review.ItemId}", It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);
        api.Setup(client => client.PostAsync<CreateReviewRequest, ReviewDto>(
                "reviews/", It.IsAny<CreateReviewRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(review);
        var service = new ReviewService(api.Object);
        var create = new CreateReviewRequest(review.RentalId, 5, "Excellent item.");

        Assert.Single(await service.GetForItemAsync(review.ItemId));
        Assert.Equal(review, await service.CreateAsync(create));
    }

    private static ItemSummaryDto CreateItemSummary(Guid itemId) => new(
        itemId, Guid.NewGuid(), "Owner", "Drill", 8m, ItemCategory.Tools,
        55.95, -3.18, true, 0, 0, null);

    private static ItemDetailDto CreateItemDetail(Guid itemId) => new(
        itemId, Guid.NewGuid(), "Owner", "Drill", "A useful test drill.", 8m,
        ItemCategory.Tools, 55.95, -3.18, true, 0, 0, DateTimeOffset.UtcNow);

    private static RentalSummaryDto CreateRental()
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);
        return new RentalSummaryDto(
            Guid.NewGuid(), Guid.NewGuid(), "Drill", Guid.NewGuid(), "Owner",
            Guid.NewGuid(), "Borrower", start, start.AddDays(1), 16m,
            RentalStatus.Requested, DateTimeOffset.UtcNow);
    }
}
