using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class RentalsViewModelTests
{
    [Fact]
    public async Task ApproveCommand_SelectedIncomingRequest_TransitionsRental()
    {
        var ownerId = Guid.NewGuid();
        var rental = CreateRental(ownerId, Guid.NewGuid(), RentalStatus.Requested);
        var rentalService = new Mock<IRentalService>();
        rentalService.Setup(service => service.GetIncomingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([rental]);
        rentalService.Setup(service => service.GetOutgoingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        rentalService.Setup(service => service.UpdateStatusAsync(
                rental.Id,
                RentalStatus.Approved,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rental with { Status = RentalStatus.Approved });
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileDto(ownerId, "Owner", "owner@test.local", 0, 0));
        var viewModel = new RentalsViewModel(
            rentalService.Object,
            authentication.Object,
            Mock.Of<INavigationService>());
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedIncomingRental = rental;

        Assert.True(viewModel.ApproveCommand.CanExecute(null));
        Assert.True(viewModel.CanApproveSelected);
        Assert.True(viewModel.CanRejectSelected);
        Assert.False(viewModel.HasOutgoingSelection);
        await viewModel.ApproveCommand.ExecuteAsync(null);

        rentalService.Verify(service => service.UpdateStatusAsync(
            rental.Id,
            RentalStatus.Approved,
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("Approved", viewModel.ConfirmationMessage!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BorrowerSelection_OnlyReturnActionIsAvailable()
    {
        var ownerId = Guid.NewGuid();
        var borrowerId = Guid.NewGuid();
        var rental = CreateRental(ownerId, borrowerId, RentalStatus.OutForRent);
        var rentalService = new Mock<IRentalService>();
        rentalService.Setup(service => service.GetIncomingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        rentalService.Setup(service => service.GetOutgoingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([rental]);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileDto(borrowerId, "Borrower", "borrower@test.local", 0, 0));
        var viewModel = new RentalsViewModel(
            rentalService.Object,
            authentication.Object,
            Mock.Of<INavigationService>());
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedOutgoingRental = rental;

        Assert.True(viewModel.HasOutgoingSelection);
        Assert.True(viewModel.CanReturnSelected);
        Assert.True(viewModel.MarkReturnedCommand.CanExecute(null));
        Assert.False(viewModel.CanApproveSelected);
        Assert.False(viewModel.CanRejectSelected);
        Assert.False(viewModel.CanStartSelected);
        Assert.False(viewModel.CanCompleteSelected);
    }

    [Fact]
    public async Task SelectingOutgoingRental_ClearsIncomingOwnerSelection()
    {
        var ownerId = Guid.NewGuid();
        var borrowerId = Guid.NewGuid();
        var incoming = CreateRental(ownerId, Guid.NewGuid(), RentalStatus.Requested);
        var outgoing = CreateRental(Guid.NewGuid(), borrowerId, RentalStatus.Approved);
        var rentalService = new Mock<IRentalService>();
        rentalService.Setup(service => service.GetIncomingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([incoming]);
        rentalService.Setup(service => service.GetOutgoingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([outgoing]);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileDto(ownerId, "User", "user@test.local", 0, 0));
        var viewModel = new RentalsViewModel(
            rentalService.Object,
            authentication.Object,
            Mock.Of<INavigationService>());
        await viewModel.LoadCommand.ExecuteAsync(null);

        viewModel.SelectedIncomingRental = incoming;
        viewModel.SelectedOutgoingRental = outgoing;

        Assert.Null(viewModel.SelectedIncomingRental);
        Assert.Same(outgoing, viewModel.SelectedOutgoingRental);
        Assert.False(viewModel.HasIncomingSelection);
        Assert.True(viewModel.HasOutgoingSelection);
    }

    [Fact]
    public async Task CompletedOutgoingRental_ReviewCommandNavigatesToExactRental()
    {
        var borrowerId = Guid.NewGuid();
        var completed = CreateRental(Guid.NewGuid(), borrowerId, RentalStatus.Completed);
        var rentalService = new Mock<IRentalService>();
        rentalService.Setup(service => service.GetIncomingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        rentalService.Setup(service => service.GetOutgoingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([completed]);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileDto(borrowerId, "Borrower", "borrower@test.local", 0, 0));
        var navigation = new Mock<INavigationService>();
        navigation.Setup(service => service.GoToAsync(
                AppRoutes.Reviews,
                It.IsAny<IReadOnlyDictionary<string, object>>()))
            .Returns(Task.CompletedTask);
        var viewModel = new RentalsViewModel(
            rentalService.Object,
            authentication.Object,
            navigation.Object);
        await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.SelectedOutgoingRental = completed;

        Assert.True(viewModel.CanReviewSelected);
        await viewModel.ReviewCommand.ExecuteAsync(null);

        navigation.Verify(service => service.GoToAsync(
            AppRoutes.Reviews,
            It.Is<IReadOnlyDictionary<string, object>>(parameters =>
                (Guid)parameters["itemId"] == completed.ItemId
                && (Guid)parameters["rentalId"] == completed.Id)), Times.Once);
    }

    private static RentalSummaryDto CreateRental(Guid ownerId, Guid borrowerId, RentalStatus status) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Tent",
        ownerId,
        "Owner",
        borrowerId,
        "Borrower",
        DateTimeOffset.UtcNow.AddDays(1),
        DateTimeOffset.UtcNow.AddDays(2),
        28m,
        status,
        DateTimeOffset.UtcNow);
}
