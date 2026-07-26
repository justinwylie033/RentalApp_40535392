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
        service.Setup(candidate => candidate.GetAllAsync(
                null,
                string.Empty,
                ItemSortOrder.Newest,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ItemSummaryDto>([expected], 1, 20, 1));
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
        service.Setup(candidate => candidate.GetAllAsync(
                null,
                string.Empty,
                ItemSortOrder.Newest,
                1,
                20,
                It.IsAny<CancellationToken>()))
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

    [Fact]
    public async Task LoadCommand_SearchAndSortSelected_ForwardsCatalogueQuery()
    {
        var service = new Mock<IItemService>();
        service.Setup(candidate => candidate.GetAllAsync(
                ItemCategory.Tools,
                "drill",
                ItemSortOrder.PriceLowToHigh,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ItemSummaryDto>([], 1, 20, 0));
        var viewModel = new ItemsListViewModel(service.Object, Mock.Of<INavigationService>())
        {
            SelectedCategory = nameof(ItemCategory.Tools),
            SearchTerm = "drill",
            SelectedSortOrder = ItemSortOrder.PriceLowToHigh
        };

        await viewModel.LoadCommand.ExecuteAsync(null);

        service.VerifyAll();
        Assert.Empty(viewModel.Items);
    }

    [Fact]
    public async Task LoadMoreCommand_MorePages_AppendsNextPage()
    {
        var first = new ItemSummaryDto(
            Guid.NewGuid(), Guid.NewGuid(), "Owner", "Drill", 8m, ItemCategory.Tools,
            55.95, -3.18, true, 0, 0, null);
        var second = first with { Id = Guid.NewGuid(), Title = "Saw" };
        var service = new Mock<IItemService>();
        service.Setup(candidate => candidate.GetAllAsync(
                null, string.Empty, ItemSortOrder.Newest, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ItemSummaryDto>([first], 1, 1, 2));
        service.Setup(candidate => candidate.GetAllAsync(
                null, string.Empty, ItemSortOrder.Newest, 2, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ItemSummaryDto>([second], 2, 1, 2));
        var viewModel = new ItemsListViewModel(service.Object, Mock.Of<INavigationService>());

        await viewModel.LoadCommand.ExecuteAsync(null);
        await viewModel.LoadMoreCommand.ExecuteAsync(null);

        Assert.Equal([first.Id, second.Id], viewModel.Items.Select(item => item.Id));
        Assert.Equal(2, viewModel.TotalResults);
        Assert.False(viewModel.HasMoreItems);
    }
}
