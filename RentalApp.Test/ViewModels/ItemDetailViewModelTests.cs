using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class ItemDetailViewModelTests
{
    [Fact]
    public async Task LoadAsync_CurrentUserOwnsItem_EnablesOwnerEditing()
    {
        var ownerId = Guid.NewGuid();
        var item = CreateItem(ownerId);
        var items = new Mock<IItemService>();
        items.Setup(service => service.GetAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileDto(ownerId, "Owner", "owner@test.local", 0, 0));
        var viewModel = new ItemDetailViewModel(
            items.Object,
            Mock.Of<IRentalService>(),
            authentication.Object,
            Mock.Of<IDeviceLocationService>(),
            Mock.Of<IAddressGeocodingService>(),
            Mock.Of<INavigationService>());

        await viewModel.LoadAsync(item.Id);

        Assert.True(viewModel.IsOwner);
        Assert.False(viewModel.CanRequestRental);
        Assert.Equal(item.Title, viewModel.EditTitle);
    }

    [Fact]
    public async Task SaveChangesCommand_OwnerEditsListing_UpdatesItem()
    {
        var ownerId = Guid.NewGuid();
        var item = CreateItem(ownerId);
        var updated = item with { Title = "Professional drill kit", DailyRate = 11m };
        var items = new Mock<IItemService>();
        items.Setup(service => service.GetAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        items.Setup(service => service.UpdateAsync(
                item.Id,
                It.IsAny<UpdateItemRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileDto(ownerId, "Owner", "owner@test.local", 0, 0));
        var viewModel = new ItemDetailViewModel(
            items.Object,
            Mock.Of<IRentalService>(),
            authentication.Object,
            Mock.Of<IDeviceLocationService>(),
            Mock.Of<IAddressGeocodingService>(),
            Mock.Of<INavigationService>());
        await viewModel.LoadAsync(item.Id);
        viewModel.BeginEditCommand.Execute(null);
        viewModel.EditTitle = updated.Title;
        viewModel.EditDailyRate = updated.DailyRate;

        await viewModel.SaveChangesCommand.ExecuteAsync(null);

        items.Verify(service => service.UpdateAsync(
            item.Id,
            It.Is<UpdateItemRequest>(request =>
                request.Title == updated.Title && request.DailyRate == updated.DailyRate),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(updated.Title, viewModel.Item?.Title);
        Assert.False(viewModel.IsEditing);
        Assert.Equal("Item changes saved.", viewModel.ConfirmationMessage);
    }

    [Fact]
    public async Task RequestRentalCommand_EndBeforeStart_ShowsValidationMessage()
    {
        var ownerId = Guid.NewGuid();
        var item = CreateItem(ownerId);
        var items = new Mock<IItemService>();
        items.Setup(service => service.GetAsync(item.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfileDto(Guid.NewGuid(), "Borrower", "borrower@test.local", 0, 0));
        var rentals = new Mock<IRentalService>();
        var viewModel = new ItemDetailViewModel(
            items.Object,
            rentals.Object,
            authentication.Object,
            Mock.Of<IDeviceLocationService>(),
            Mock.Of<IAddressGeocodingService>(),
            Mock.Of<INavigationService>());
        await viewModel.LoadAsync(item.Id);
        viewModel.StartDate = DateTime.Today.AddDays(3);
        viewModel.EndDate = DateTime.Today.AddDays(2);

        await viewModel.RequestRentalCommand.ExecuteAsync(null);

        Assert.Equal("The end date cannot be before the start date.", viewModel.ErrorMessage);
        rentals.Verify(service => service.RequestAsync(
            It.IsAny<CreateRentalRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static ItemDetailDto CreateItem(Guid ownerId) => new(
        Guid.NewGuid(),
        ownerId,
        "Owner",
        "Cordless drill",
        "Reliable drill with two batteries.",
        9m,
        ItemCategory.Tools,
        55.9533,
        -3.1883,
        true,
        4.5,
        2,
        DateTimeOffset.UtcNow);
}
