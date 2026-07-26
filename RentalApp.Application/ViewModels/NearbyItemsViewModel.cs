using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

public partial class NearbyItemsViewModel(
    IItemService items,
    IDeviceLocationService location,
    INavigationService navigation) : ViewModelBase
{
    public ObservableCollection<ItemSummaryDto> Items { get; } = [];
    public IReadOnlyList<string> Categories { get; } = ["All", .. Enum.GetNames<ItemCategory>()];

    [ObservableProperty]
    private double radiusKilometres = 5;

    [ObservableProperty]
    private string selectedCategory = "All";

    [ObservableProperty]
    private string locationSummary = "Location not requested";

    [RelayCommand]
    private Task SearchAsync() => RunBusyAsync(async () =>
    {
        var current = await location.GetCurrentAsync();
        LocationSummary = $"{current.Latitude:F4}, {current.Longitude:F4}";
        ItemCategory? category = Enum.TryParse<ItemCategory>(SelectedCategory, out var parsed) ? parsed : null;
        var results = await items.FindNearbyAsync(
            current.Latitude,
            current.Longitude,
            RadiusKilometres,
            category);
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
