using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

public partial class ReviewsViewModel(IReviewService reviews, IRentalService rentals) : ViewModelBase
{
    private Guid _itemId;
    private Guid? _preferredRentalId;

    public ObservableCollection<ReviewDto> Reviews { get; } = [];

    public ObservableCollection<RentalSummaryDto> EligibleRentals { get; } = [];

    [ObservableProperty]
    private RentalSummaryDto? selectedRental;

    [ObservableProperty]
    private int rating = 5;

    [ObservableProperty]
    private string comment = string.Empty;

    [ObservableProperty]
    private string? confirmationMessage;

    public Task LoadAsync(Guid itemId, Guid? preferredRentalId = null)
    {
        _itemId = itemId;
        _preferredRentalId = preferredRentalId;
        return RefreshAsync();
    }

    [RelayCommand]
    private Task SubmitAsync() => RunBusyAsync(async () =>
    {
        var selected = SelectedRental
            ?? throw new InvalidOperationException("Select a completed rental before submitting a review.");

        await reviews.CreateAsync(new CreateReviewRequest(selected.Id, Rating, Comment));
        Comment = string.Empty;
        ConfirmationMessage = "Review submitted.";
        await RefreshWithoutBusyGuardAsync();
    });

    [RelayCommand]
    private Task RefreshAsync() => RunBusyAsync(RefreshWithoutBusyGuardAsync);

    private async Task RefreshWithoutBusyGuardAsync()
    {
        var reviewsTask = reviews.GetForItemAsync(_itemId);
        var rentalsTask = rentals.GetOutgoingAsync();
        await Task.WhenAll(reviewsTask, rentalsTask);

        Reviews.Clear();
        foreach (var review in await reviewsTask)
        {
            Reviews.Add(review);
        }

        var reviewedRentalIds = Reviews.Select(review => review.RentalId).ToHashSet();
        EligibleRentals.Clear();
        foreach (var rental in (await rentalsTask).Where(rental =>
                     rental.ItemId == _itemId
                     && rental.Status == RentalStatus.Completed
                     && !reviewedRentalIds.Contains(rental.Id)))
        {
            EligibleRentals.Add(rental);
        }

        SelectedRental = EligibleRentals.FirstOrDefault(rental => rental.Id == _preferredRentalId)
            ?? EligibleRentals.FirstOrDefault();
    }
}
