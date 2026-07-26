using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class MyListingsViewModelTests
{
    [Fact]
    public async Task LoadCommand_OwnedListingsReturned_PopulatesManagementList()
    {
        var available = CreateItem("Drill", true);
        var unavailable = CreateItem("Tent", false);
        var service = new Mock<IItemService>();
        service.Setup(candidate => candidate.GetMineAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([available, unavailable]);
        var viewModel = new MyListingsViewModel(service.Object, Mock.Of<INavigationService>());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.Items.Count);
        Assert.Contains(viewModel.Items, item => !item.IsAvailable);
    }

    [Fact]
    public async Task OpenItemCommand_ListingSelected_OpensExistingDetailRoute()
    {
        var item = CreateItem("Drill", true);
        var navigation = new Mock<INavigationService>();
        var viewModel = new MyListingsViewModel(Mock.Of<IItemService>(), navigation.Object);

        await viewModel.OpenItemCommand.ExecuteAsync(item);

        navigation.Verify(candidate => candidate.GoToAsync(
            AppRoutes.ItemDetail,
            It.Is<IReadOnlyDictionary<string, object>>(values => (Guid)values["itemId"] == item.Id)));
    }

    private static ItemSummaryDto CreateItem(string title, bool isAvailable) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Owner",
        title,
        8m,
        ItemCategory.Tools,
        55.95,
        -3.18,
        isAvailable,
        0,
        0,
        null);
}
