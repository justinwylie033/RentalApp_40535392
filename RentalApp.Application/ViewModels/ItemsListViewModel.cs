using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

public partial class ItemsListViewModel(IItemService items, INavigationService navigation) : ViewModelBase
{
    private const int PageSize = 20;
    private int _currentPage = 1;

    public ObservableCollection<ItemSummaryDto> Items { get; } = [];
    public IReadOnlyList<string> Categories { get; } = ["All", .. Enum.GetNames<ItemCategory>()];
    public IReadOnlyList<ItemSortOrder> SortOrders { get; } = Enum.GetValues<ItemSortOrder>();

    [ObservableProperty]
    private string selectedCategory = "All";

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private ItemSortOrder selectedSortOrder = ItemSortOrder.Newest;

    [ObservableProperty]
    private int totalResults;

    [ObservableProperty]
    private bool hasMoreItems;

    [RelayCommand]
    private Task LoadAsync() => LoadPageAsync(reset: true);

    [RelayCommand]
    private Task LoadMoreAsync() => HasMoreItems
        ? LoadPageAsync(reset: false)
        : Task.CompletedTask;

    private Task LoadPageAsync(bool reset) => RunBusyAsync(async () =>
    {
        if (reset)
        {
            _currentPage = 1;
        }

        ItemCategory? category = Enum.TryParse<ItemCategory>(SelectedCategory, out var parsed) ? parsed : null;
        var result = await items.GetAllAsync(
            category,
            SearchTerm,
            SelectedSortOrder,
            _currentPage,
            PageSize);
        if (reset)
        {
            Items.Clear();
        }

        foreach (var item in result.Items)
        {
            Items.Add(item);
        }

        TotalResults = result.TotalCount;
        HasMoreItems = result.HasNextPage;
        if (HasMoreItems)
        {
            _currentPage++;
        }
    });

    [RelayCommand]
    private Task OpenItemAsync(ItemSummaryDto? item) => item is null
        ? Task.CompletedTask
        : RunBusyAsync(() => navigation.GoToAsync(
            AppRoutes.ItemDetail,
            new Dictionary<string, object> { ["itemId"] = item.Id }));
}
