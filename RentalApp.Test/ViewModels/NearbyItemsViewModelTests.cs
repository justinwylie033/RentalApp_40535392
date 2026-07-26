using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class NearbyItemsViewModelTests
{
    [Fact]
    public async Task SearchCommand_DeviceLocationAndRadius_ForwardsSearchParameters()
    {
        var itemService = new Mock<IItemService>();
        itemService.Setup(service => service.FindNearbyAsync(55.9533, -3.1883, 5, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var location = new Mock<IDeviceLocationService>();
        location.Setup(service => service.GetCurrentAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeoCoordinate(55.9533, -3.1883));
        var viewModel = new NearbyItemsViewModel(itemService.Object, location.Object, Mock.Of<INavigationService>());

        await viewModel.SearchCommand.ExecuteAsync(null);

        itemService.VerifyAll();
        Assert.Equal("55.9533, -3.1883", viewModel.LocationSummary);
    }
}
