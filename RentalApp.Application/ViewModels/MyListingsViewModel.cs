using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

/// <summary>
/// Presents every listing owned by the current account, including listings hidden
/// from Browse, so availability and listing details remain manageable.
/// </summary>
public partial class MyListingsViewModel(
    IItemService items,
    INavigationService navigation) : ViewModelBase
{
    public ObservableCollection<ItemSummaryDto> Items { get; } = [];

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async () =>
    {
        var ownedItems = await items.GetMineAsync();
        Items.Clear();
        foreach (var item in ownedItems)
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
