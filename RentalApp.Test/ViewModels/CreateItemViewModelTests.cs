using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class CreateItemViewModelTests
{
    [Fact]
    public async Task UseCurrentLocationCommand_LocationAvailable_UpdatesAddress()
    {
        var location = new Mock<IDeviceLocationService>();
        location.Setup(service => service.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoCoordinate(55.94, -3.20));
        var geocoding = new Mock<IAddressGeocodingService>();
        geocoding.Setup(service => service.ReverseAsync(55.94, -3.20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedAddress("10 Test Street, Edinburgh, EH1 1AA", 55.94, -3.20));
        var viewModel = new CreateItemViewModel(
            Mock.Of<IItemService>(), location.Object, geocoding.Object, Mock.Of<INavigationService>());

        await viewModel.UseCurrentLocationCommand.ExecuteAsync(null);

        Assert.Equal("10 Test Street, Edinburgh, EH1 1AA", viewModel.Address);
        Assert.Contains("Address found", viewModel.LocationConfirmation);
    }

    [Fact]
    public async Task SaveCommand_ValidForm_CreatesItemAndClearsForm()
    {
        var service = new Mock<IItemService>();
        service.Setup(candidate => candidate.CreateAsync(It.IsAny<CreateItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemDetailDto(
                Guid.NewGuid(), Guid.NewGuid(), "Owner", "Drill", "Good drill for DIY work.",
                7m, ItemCategory.Tools, 55.95, -3.18, true, 0, 0, DateTimeOffset.UtcNow));
        var navigation = new Mock<INavigationService>();
        var geocoding = new Mock<IAddressGeocodingService>();
        geocoding.Setup(candidate => candidate.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResolvedAddress("10 Test Street, Edinburgh, EH1 1AA", 55.95, -3.18));
        var viewModel = new CreateItemViewModel(
            service.Object,
            Mock.Of<IDeviceLocationService>(),
            geocoding.Object,
            navigation.Object)
        {
            Title = "Drill",
            Description = "Good drill for DIY work.",
            DailyRate = 7m,
            Address = "10 Test Street, Edinburgh, EH1 1AA"
        };

        await viewModel.SaveCommand.ExecuteAsync(null);

        service.Verify(candidate => candidate.CreateAsync(
            It.Is<CreateItemRequest>(request =>
                request.Title == "Drill" &&
                request.DailyRate == 7m &&
                request.Address == "10 Test Street, Edinburgh, EH1 1AA" &&
                request.Latitude == 55.95 &&
                request.Longitude == -3.18),
            It.IsAny<CancellationToken>()), Times.Once);
        navigation.Verify(candidate => candidate.GoToAsync(AppRoutes.Items, null), Times.Once);
        Assert.Equal(string.Empty, viewModel.Title);
        Assert.Equal(string.Empty, viewModel.Address);
        Assert.Equal(0, viewModel.DailyRate);
    }
}
