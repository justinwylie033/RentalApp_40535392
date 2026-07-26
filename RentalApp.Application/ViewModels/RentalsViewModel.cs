using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

public partial class RentalsViewModel : ViewModelBase
{
    private readonly IRentalService _rentals;
    private readonly IAuthenticationService _authentication;
    private readonly INavigationService _navigation;
    private Guid _currentUserId;

    public ObservableCollection<RentalSummaryDto> Incoming { get; } = [];
    public ObservableCollection<RentalSummaryDto> Outgoing { get; } = [];

    public RentalsViewModel(
        IRentalService rentals,
        IAuthenticationService authentication,
        INavigationService navigation)
    {
        _rentals = rentals;
        _authentication = authentication;
        _navigation = navigation;
    }

    [ObservableProperty]
    private RentalSummaryDto? selectedIncomingRental;

    [ObservableProperty]
    private RentalSummaryDto? selectedOutgoingRental;

    [ObservableProperty]
    private string? confirmationMessage;

    // Presentation point: every account can both list and rent items. Actions
    // depend on the user's relationship to this rental, not a permanent role.
    public bool HasIncomingSelection => SelectedIncomingRental is not null;
    public bool HasOutgoingSelection => SelectedOutgoingRental is not null;
    public bool HasAnySelection => HasIncomingSelection || HasOutgoingSelection;

    public bool CanApproveSelected =>
        IsSelectedIncoming(RentalStatus.Requested);

    public bool CanRejectSelected =>
        IsSelectedIncoming(RentalStatus.Requested, RentalStatus.Approved);

    public bool CanStartSelected =>
        IsSelectedIncoming(RentalStatus.Approved);

    public bool CanReturnSelected =>
        SelectedOutgoingRental is { } rental
        && rental.BorrowerId == _currentUserId
        && rental.Status is RentalStatus.OutForRent or RentalStatus.Overdue;

    public bool CanCompleteSelected =>
        IsSelectedIncoming(RentalStatus.Returned);

    public bool CanReviewSelected =>
        SelectedOutgoingRental is { } rental
        && rental.BorrowerId == _currentUserId
        && rental.Status == RentalStatus.Completed;

    public bool CanCancelSelected =>
        SelectedOutgoingRental is { } rental
        && rental.BorrowerId == _currentUserId
        && rental.Status is RentalStatus.Requested or RentalStatus.Approved;

    public string RequestGuidance => SelectedOutgoingRental?.Status switch
    {
        RentalStatus.Requested => "Waiting for the person who listed the item to respond.",
        RentalStatus.Approved => "Approved. Arrange collection of the item.",
        RentalStatus.OutForRent => "Return the item, then confirm it here.",
        RentalStatus.Overdue => "This rental is overdue. Return the item and confirm it here.",
        RentalStatus.Returned => "Return recorded. Waiting for final confirmation.",
        RentalStatus.Completed => "Rental complete. You can now leave a verified review.",
        RentalStatus.Rejected => "This rental request was declined.",
        RentalStatus.Cancelled => "You cancelled this rental request.",
        _ => string.Empty
    };

    public bool ListingSelectionHasNoAction =>
        HasIncomingSelection
        && !CanApproveSelected
        && !CanRejectSelected
        && !CanStartSelected
        && !CanCompleteSelected;

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async () =>
    {
        var profileTask = _authentication.GetProfileAsync();
        var incomingTask = _rentals.GetIncomingAsync();
        var outgoingTask = _rentals.GetOutgoingAsync();
        await Task.WhenAll(profileTask, incomingTask, outgoingTask);

        _currentUserId = (await profileTask).Id;
        Replace(Incoming, await incomingTask);
        Replace(Outgoing, await outgoingTask);
        ClearSelections();
    });

    [RelayCommand(CanExecute = nameof(CanApprove))]
    private Task ApproveAsync() => ChangeStatusAsync(RentalStatus.Approved);

    [RelayCommand(CanExecute = nameof(CanReject))]
    private Task RejectAsync() => ChangeStatusAsync(RentalStatus.Rejected);

    [RelayCommand(CanExecute = nameof(CanMarkOutForRent))]
    private Task MarkOutForRentAsync() => ChangeStatusAsync(RentalStatus.OutForRent);

    [RelayCommand(CanExecute = nameof(CanMarkReturned))]
    private Task MarkReturnedAsync() => ChangeStatusAsync(RentalStatus.Returned);

    [RelayCommand(CanExecute = nameof(CanComplete))]
    private Task CompleteAsync() => ChangeStatusAsync(RentalStatus.Completed);

    [RelayCommand(CanExecute = nameof(CanReview))]
    private Task ReviewAsync() => RunBusyAsync(async () =>
    {
        var selected = SelectedOutgoingRental
            ?? throw new InvalidOperationException("Select a completed outgoing rental first.");

        await _navigation.GoToAsync(
            AppRoutes.Reviews,
            new Dictionary<string, object>
            {
                ["itemId"] = selected.ItemId,
                ["rentalId"] = selected.Id
            });
    });

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private Task CancelAsync() => ChangeStatusAsync(RentalStatus.Cancelled);

    private bool CanApprove() => CanApproveSelected;

    private bool CanReject() => CanRejectSelected;

    private bool CanMarkOutForRent() => CanStartSelected;

    private bool CanMarkReturned() => CanReturnSelected;

    private bool CanComplete() => CanCompleteSelected;

    private bool CanReview() => CanReviewSelected;

    private bool CanCancel() => CanCancelSelected;

    private Task ChangeStatusAsync(RentalStatus status) => RunBusyAsync(async () =>
    {
        var selected = SelectedIncomingRental ?? SelectedOutgoingRental
            ?? throw new InvalidOperationException("Select a rental first.");

        var updated = await _rentals.UpdateStatusAsync(selected.Id, status);
        ConfirmationMessage = $"{updated.ItemTitle} is now {updated.Status}.";
        await LoadWithoutBusyGuardAsync();
    });

    private async Task LoadWithoutBusyGuardAsync()
    {
        var selectedIncomingId = SelectedIncomingRental?.Id;
        var selectedOutgoingId = SelectedOutgoingRental?.Id;
        var incoming = await _rentals.GetIncomingAsync();
        var outgoing = await _rentals.GetOutgoingAsync();
        Replace(Incoming, incoming);
        Replace(Outgoing, outgoing);

        // Preserve the selected workflow after a transition so the user sees the
        // new status, confirmation, and next role-appropriate instruction.
        if (selectedIncomingId is not null)
        {
            SelectedIncomingRental = Incoming.FirstOrDefault(rental => rental.Id == selectedIncomingId);
        }
        else if (selectedOutgoingId is not null)
        {
            SelectedOutgoingRental = Outgoing.FirstOrDefault(rental => rental.Id == selectedOutgoingId);
        }
    }

    partial void OnSelectedIncomingRentalChanged(RentalSummaryDto? value)
    {
        if (value is not null)
        {
            SelectedOutgoingRental = null;
        }

        NotifyActionStateChanged();
    }

    partial void OnSelectedOutgoingRentalChanged(RentalSummaryDto? value)
    {
        if (value is not null)
        {
            SelectedIncomingRental = null;
        }

        NotifyActionStateChanged();
    }

    private bool IsSelectedIncoming(params RentalStatus[] statuses) =>
        SelectedIncomingRental is { } rental
        && rental.OwnerId == _currentUserId
        && statuses.Contains(rental.Status);

    private void ClearSelections()
    {
        SelectedIncomingRental = null;
        SelectedOutgoingRental = null;
    }

    private void NotifyActionStateChanged()
    {
        OnPropertyChanged(nameof(HasIncomingSelection));
        OnPropertyChanged(nameof(HasOutgoingSelection));
        OnPropertyChanged(nameof(HasAnySelection));
        OnPropertyChanged(nameof(CanApproveSelected));
        OnPropertyChanged(nameof(CanRejectSelected));
        OnPropertyChanged(nameof(CanStartSelected));
        OnPropertyChanged(nameof(CanReturnSelected));
        OnPropertyChanged(nameof(CanCompleteSelected));
        OnPropertyChanged(nameof(CanReviewSelected));
        OnPropertyChanged(nameof(CanCancelSelected));
        OnPropertyChanged(nameof(RequestGuidance));
        OnPropertyChanged(nameof(ListingSelectionHasNoAction));

        ApproveCommand.NotifyCanExecuteChanged();
        RejectCommand.NotifyCanExecuteChanged();
        MarkOutForRentCommand.NotifyCanExecuteChanged();
        MarkReturnedCommand.NotifyCanExecuteChanged();
        CompleteCommand.NotifyCanExecuteChanged();
        ReviewCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    private static void Replace(
        ObservableCollection<RentalSummaryDto> destination,
        IEnumerable<RentalSummaryDto> source)
    {
        destination.Clear();
        foreach (var rental in source)
        {
            destination.Add(rental);
        }
    }
}
