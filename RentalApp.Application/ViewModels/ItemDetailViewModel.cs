using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

public partial class ItemDetailViewModel(
    IItemService items,
    IRentalService rentals,
    IAuthenticationService authentication,
    IDeviceLocationService location,
    IAddressGeocodingService geocoding,
    INavigationService navigation) : ViewModelBase
{
    // Presentation point: a single detail ViewModel exposes different commands based
    // on authenticated ownership, but the API still enforces every security rule.
    private string resolvedEditAddressInput = string.Empty;
    private double editLatitude;
    private double editLongitude;

    public IReadOnlyList<ItemCategory> Categories { get; } = Enum.GetValues<ItemCategory>();

    public DateTime MinimumRentalDate => DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRequestRental))]
    private ItemDetailDto? item;

    [ObservableProperty]
    private DateTime startDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private DateTime endDate = DateTime.Today.AddDays(2);

    [ObservableProperty]
    private string? confirmationMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRequestRental))]
    private bool isOwner;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsViewing))]
    private bool isEditing;

    [ObservableProperty]
    private string editTitle = string.Empty;

    [ObservableProperty]
    private string editDescription = string.Empty;

    [ObservableProperty]
    private decimal editDailyRate;

    [ObservableProperty]
    private ItemCategory editCategory = ItemCategory.Tools;

    [ObservableProperty]
    private string editAddress = string.Empty;

    [ObservableProperty]
    private string? editLocationConfirmation;

    [ObservableProperty]
    private bool editIsAvailable = true;

    public bool IsViewing => !IsEditing;

    public bool CanRequestRental => !IsOwner && Item?.IsAvailable == true;

    public Task LoadAsync(Guid itemId) => RunBusyAsync(async () =>
    {
        // Fetch item and profile concurrently to reduce the detail-screen wait time.
        var itemTask = items.GetAsync(itemId);
        var profileTask = authentication.GetProfileAsync();
        await Task.WhenAll(itemTask, profileTask);

        var loadedItem = await itemTask;
        var profile = await profileTask;
        Item = loadedItem;
        IsOwner = loadedItem.OwnerId == profile.Id;
        IsEditing = false;
        CopyItemToEditor();
    });

    [RelayCommand]
    private void BeginEdit()
    {
        if (!IsOwner || Item is null)
        {
            return;
        }

        CopyItemToEditor();
        ConfirmationMessage = null;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        CopyItemToEditor();
        IsEditing = false;
    }

    [RelayCommand]
    private Task FindEditAddressAsync() => RunBusyAsync(ResolveTypedEditAddressAsync);

    [RelayCommand]
    private Task UseCurrentLocationForEditAsync() => RunBusyAsync(async () =>
    {
        var current = await location.GetCurrentAsync();
        ApplyResolvedEditAddress(await geocoding.ReverseAsync(current.Latitude, current.Longitude));
    });

    [RelayCommand]
    private Task SaveChangesAsync() => RunBusyAsync(async () =>
    {
        if (!IsOwner || Item is not { } currentItem)
        {
            throw new InvalidOperationException("Only the owner can update this item.");
        }

        if (string.IsNullOrWhiteSpace(EditAddress) ||
            !string.Equals(EditAddress.Trim(), resolvedEditAddressInput, StringComparison.OrdinalIgnoreCase))
        {
            await ResolveTypedEditAddressAsync();
        }

        // The readable address and resolved coordinate are sent as one DTO so the
        // API can persist both representations atomically.
        Item = await items.UpdateAsync(currentItem.Id, new UpdateItemRequest(
            EditTitle,
            EditDescription,
            EditDailyRate,
            EditCategory,
            editLatitude,
            editLongitude,
            EditIsAvailable,
            EditAddress));
        IsEditing = false;
        ConfirmationMessage = "Item changes saved.";
    });

    [RelayCommand]
    private Task RequestRentalAsync() => RunBusyAsync(async () =>
    {
        if (Item is not { } currentItem)
        {
            throw new InvalidOperationException("Load an item before requesting a rental.");
        }

        if (EndDate.Date < StartDate.Date)
        {
            throw new InvalidOperationException("The end date cannot be before the start date.");
        }

        // DatePickers use local dates; the API contract uses UTC for consistent
        // overlap checks across devices and containers.
        var start = new DateTimeOffset(StartDate.Date, TimeZoneInfo.Local.GetUtcOffset(StartDate.Date)).ToUniversalTime();
        var end = new DateTimeOffset(EndDate.Date, TimeZoneInfo.Local.GetUtcOffset(EndDate.Date)).ToUniversalTime();
        var rental = await rentals.RequestAsync(new CreateRentalRequest(currentItem.Id, start, end));
        ConfirmationMessage = $"Request sent. Total price: {rental.TotalPrice:C}.";
    });

    [RelayCommand]
    private Task ViewReviewsAsync()
    {
        if (Item is not { } currentItem)
        {
            return Task.CompletedTask;
        }

        return RunBusyAsync(() => navigation.GoToAsync(
            AppRoutes.Reviews,
            new Dictionary<string, object> { ["itemId"] = currentItem.Id }));
    }

    private async Task ResolveTypedEditAddressAsync()
    {
        ApplyResolvedEditAddress(await geocoding.ResolveAsync(EditAddress));
    }

    private void ApplyResolvedEditAddress(ResolvedAddress resolved)
    {
        resolvedEditAddressInput = resolved.DisplayAddress;
        EditAddress = resolved.DisplayAddress;
        editLatitude = resolved.Latitude;
        editLongitude = resolved.Longitude;
        EditLocationConfirmation = $"Address found: {resolved.DisplayAddress}";
    }

    private void CopyItemToEditor()
    {
        if (Item is not { } currentItem)
        {
            return;
        }

        EditTitle = currentItem.Title;
        EditDescription = currentItem.Description;
        EditDailyRate = currentItem.DailyRate;
        EditCategory = currentItem.Category;
        resolvedEditAddressInput = currentItem.Address;
        EditAddress = currentItem.Address;
        editLatitude = currentItem.Latitude;
        editLongitude = currentItem.Longitude;
        EditLocationConfirmation = null;
        EditIsAvailable = currentItem.IsAvailable;
    }
}
