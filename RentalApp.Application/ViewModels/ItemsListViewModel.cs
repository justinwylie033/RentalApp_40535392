using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

public partial class ItemsListViewModel(IItemService items, INavigationService navigation) : ViewModelBase
{
    public ObservableCollection<ItemSummaryDto> Items { get; } = [];
    public IReadOnlyList<string> Categories { get; } = ["All", .. Enum.GetNames<ItemCategory>()];
    public IReadOnlyList<ItemSortOrder> SortOrders { get; } = Enum.GetValues<ItemSortOrder>();

    [ObservableProperty]
    private string selectedCategory = "All";

    [ObservableProperty]
    private string searchTerm = string.Empty;

    [ObservableProperty]
    private ItemSortOrder selectedSortOrder = ItemSortOrder.Newest;

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async () =>
    {
        ItemCategory? category = Enum.TryParse<ItemCategory>(SelectedCategory, out var parsed) ? parsed : null;
        var results = await items.GetAllAsync(category, SearchTerm, SelectedSortOrder);
        Items.Clear();
        foreach (var item in results)
        {
            Items.Add(item);
        }
    });

    [RelayCommand]
    private Task OpenItemAsync(ItemSummaryDto? item) => item is null
        ? Task.CompletedTask
        : RunBusyAsync(() => navigation.GoToAsync(
            AppRoutes.ItemDetail,
            new Dictionary<string, object> { ["itemId"] = item.Id }));
}
