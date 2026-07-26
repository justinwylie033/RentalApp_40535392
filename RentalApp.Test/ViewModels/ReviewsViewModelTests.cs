using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class ReviewsViewModelTests
{
    [Fact]
    public async Task LoadAsync_CompletedRentalForItem_OffersRentalForReview()
    {
        var itemId = Guid.NewGuid();
        var completed = CreateRental(itemId, RentalStatus.Completed);
        var otherItem = CreateRental(Guid.NewGuid(), RentalStatus.Completed);
        var reviews = new Mock<IReviewService>();
        reviews.Setup(service => service.GetForItemAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var rentals = new Mock<IRentalService>();
        rentals.Setup(service => service.GetOutgoingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([completed, otherItem]);
        var viewModel = new ReviewsViewModel(reviews.Object, rentals.Object);

        await viewModel.LoadAsync(itemId);

        Assert.Same(completed, Assert.Single(viewModel.EligibleRentals));
        Assert.Same(completed, viewModel.SelectedRental);
    }

    [Fact]
    public async Task SubmitCommand_EligibleRental_CreatesVerifiedReview()
    {
        var itemId = Guid.NewGuid();
        var completed = CreateRental(itemId, RentalStatus.Completed);
        var reviews = new Mock<IReviewService>();
        reviews.Setup(service => service.GetForItemAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        reviews.Setup(service => service.CreateAsync(
                It.IsAny<CreateReviewRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReviewDto(
                Guid.NewGuid(), completed.Id, itemId, completed.BorrowerId,
                completed.BorrowerName, 5, "Excellent item.", DateTimeOffset.UtcNow));
        var rentals = new Mock<IRentalService>();
        rentals.Setup(service => service.GetOutgoingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([completed]);
        var viewModel = new ReviewsViewModel(reviews.Object, rentals.Object)
        {
            Rating = 5,
            Comment = "Excellent item."
        };
        await viewModel.LoadAsync(itemId);

        await viewModel.SubmitCommand.ExecuteAsync(null);

        reviews.Verify(service => service.CreateAsync(
            It.Is<CreateReviewRequest>(request =>
                request.RentalId == completed.Id && request.Rating == 5),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("Review submitted.", viewModel.ConfirmationMessage);
    }

    [Fact]
    public async Task LoadAsync_PreferredCompletedRental_SelectsRequestedWorkflowRental()
    {
        var itemId = Guid.NewGuid();
        var first = CreateRental(itemId, RentalStatus.Completed);
        var preferred = CreateRental(itemId, RentalStatus.Completed);
        var reviews = new Mock<IReviewService>();
        reviews.Setup(service => service.GetForItemAsync(itemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var rentals = new Mock<IRentalService>();
        rentals.Setup(service => service.GetOutgoingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([first, preferred]);
        var viewModel = new ReviewsViewModel(reviews.Object, rentals.Object);

        await viewModel.LoadAsync(itemId, preferred.Id);

        Assert.Same(preferred, viewModel.SelectedRental);
    }

    private static RentalSummaryDto CreateRental(Guid itemId, RentalStatus status) => new(
        Guid.NewGuid(),
        itemId,
        "Drill",
        Guid.NewGuid(),
        "Owner",
        Guid.NewGuid(),
        "Borrower",
        DateTimeOffset.UtcNow.AddDays(-3),
        DateTimeOffset.UtcNow.AddDays(-2),
        18m,
        status,
        DateTimeOffset.UtcNow.AddDays(-4));
}
