using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class ItemsListViewModelTests
{
    [Fact]
    public async Task LoadCommand_ServiceReturnsItems_PopulatesCollection()
    {
        var expected = new ItemSummaryDto(
            Guid.NewGuid(), Guid.NewGuid(), "Owner", "Drill", 8m, ItemCategory.Tools,
            55.95, -3.18, true, 4.5, 3, null);
        var service = new Mock<IItemService>();
        service.Setup(candidate => candidate.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([expected]);
        var viewModel = new ItemsListViewModel(service.Object, Mock.Of<INavigationService>());

        await viewModel.LoadCommand.ExecuteAsync(null);

        var actual = Assert.Single(viewModel.Items);
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(3, actual.ReviewCount);
        Assert.Null(viewModel.ErrorMessage);
        Assert.False(viewModel.IsBusy);
    }

    [Fact]
    public async Task LoadCommand_ServiceFails_DisplaysErrorWithoutThrowing()
    {
        var service = new Mock<IItemService>();
        service.Setup(candidate => candidate.GetAllAsync(null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API unavailable"));
        var viewModel = new ItemsListViewModel(service.Object, Mock.Of<INavigationService>());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal("API unavailable", viewModel.ErrorMessage);
        Assert.Empty(viewModel.Items);
    }

    [Fact]
    public async Task OpenItemCommand_SelectedItem_NavigatesWithItemId()
    {
        var navigation = new Mock<INavigationService>();
        var viewModel = new ItemsListViewModel(Mock.Of<IItemService>(), navigation.Object);
        var item = new ItemSummaryDto(
            Guid.NewGuid(), Guid.NewGuid(), "Owner", "Tent", 10m, ItemCategory.Camping,
            55.95, -3.18, true, 0, 0, null);

        await viewModel.OpenItemCommand.ExecuteAsync(item);

        navigation.Verify(candidate => candidate.GoToAsync(
            AppRoutes.ItemDetail,
            It.Is<IReadOnlyDictionary<string, object>>(values => (Guid)values["itemId"] == item.Id)), Times.Once);
    }
}
