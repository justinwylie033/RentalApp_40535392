using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

public partial class CreateItemViewModel : ViewModelBase
{
    private readonly IItemService _items;
    private readonly IDeviceLocationService _location;
    private readonly IAddressGeocodingService _geocoding;
    private readonly INavigationService _navigation;

    // Presentation point: the ViewModel holds UI state and commands only. Listing
    // persistence and platform geocoding are accessed through injected interfaces.
    private string _resolvedAddressInput = string.Empty;
    private double _latitude;
    private double _longitude;

    public CreateItemViewModel(
        IItemService items,
        IDeviceLocationService location,
        IAddressGeocodingService geocoding,
        INavigationService navigation)
    {
        _items = items;
        _location = location;
        _geocoding = geocoding;
        _navigation = navigation;
    }

    public IReadOnlyList<ItemCategory> Categories { get; } = Enum.GetValues<ItemCategory>();

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private decimal dailyRate;

    [ObservableProperty]
    private ItemCategory selectedCategory = ItemCategory.Tools;

    [ObservableProperty]
    private string address = string.Empty;

    [ObservableProperty]
    private string? locationConfirmation;

    [RelayCommand]
    private Task FindAddressAsync()
    {
        return RunBusyAsync(ResolveTypedAddressAsync);
    }

    [RelayCommand]
    private Task UseCurrentLocationAsync()
    {
        return RunBusyAsync(UseCurrentLocationCoreAsync);
    }

    private async Task UseCurrentLocationCoreAsync()
    {
        // GPS is reverse-geocoded so the user sees a normal collection address,
        // while the hidden coordinate remains available for PostGIS.
        var currentLocation = await _location.GetCurrentAsync();
        var resolvedAddress = await _geocoding.ReverseAsync(
            currentLocation.Latitude,
            currentLocation.Longitude);

        ApplyResolvedAddress(resolvedAddress);
    }

    [RelayCommand]
    private Task SaveAsync()
    {
        return RunBusyAsync(SaveCoreAsync);
    }

    private async Task SaveCoreAsync()
    {
        // Re-resolve whenever the text changed after the last successful lookup;
        // this prevents an address label and spatial point from drifting apart.
        if (string.IsNullOrWhiteSpace(Address) ||
            !string.Equals(Address.Trim(), _resolvedAddressInput, StringComparison.OrdinalIgnoreCase))
        {
            await ResolveTypedAddressAsync();
        }

        var request = new CreateItemRequest(
            Title,
            Description,
            DailyRate,
            SelectedCategory,
            _latitude,
            _longitude,
            Address);

        await _items.CreateAsync(request);
        ClearForm();
        // Navigation is abstracted so this workflow can be unit tested without MAUI.
        await _navigation.GoToAsync(AppRoutes.Items);
    }

    private async Task ResolveTypedAddressAsync()
    {
        var resolvedAddress = await _geocoding.ResolveAsync(Address);
        ApplyResolvedAddress(resolvedAddress);
    }

    private void ApplyResolvedAddress(ResolvedAddress resolved)
    {
        _resolvedAddressInput = resolved.DisplayAddress;
        Address = resolved.DisplayAddress;
        _latitude = resolved.Latitude;
        _longitude = resolved.Longitude;
        LocationConfirmation = $"Address found: {resolved.DisplayAddress}";
    }

    private void ClearForm()
    {
        Title = string.Empty;
        Description = string.Empty;
        DailyRate = 0;
        SelectedCategory = ItemCategory.Tools;
        _resolvedAddressInput = string.Empty;
        Address = string.Empty;
        LocationConfirmation = null;
    }
}
