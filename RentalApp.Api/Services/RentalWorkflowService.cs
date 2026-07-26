using RentalApp.Contracts;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;
using RentalApp.Database.States;

namespace RentalApp.Api.Services;

/// <summary>Defines the authoritative rental workflow use cases.</summary>
public interface IRentalWorkflowService
{
    /// <summary>Validates and creates a rental request with inclusive pricing.</summary>
    Task<RentalSummaryDto> RequestAsync(Guid borrowerId, CreateRentalRequest request, CancellationToken cancellationToken = default);
    /// <summary>Returns rental requests for the current user's listings.</summary>
    Task<IReadOnlyList<RentalSummaryDto>> GetIncomingAsync(Guid ownerId, CancellationToken cancellationToken = default);
    /// <summary>Returns rentals requested by the current user.</summary>
    Task<IReadOnlyList<RentalSummaryDto>> GetOutgoingAsync(Guid borrowerId, CancellationToken cancellationToken = default);
    /// <summary>Applies a role-authorised and state-valid transition.</summary>
    Task<RentalSummaryDto> TransitionAsync(Guid userId, Guid rentalId, RentalStatus nextStatus, CancellationToken cancellationToken = default);
    /// <summary>Moves expired out-for-rent records into the overdue state.</summary>
    Task<int> MarkOverdueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed class RentalWorkflowService : IRentalWorkflowService
{
    private readonly IRentalRepository _rentals;
    private readonly IItemRepository _items;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RentalStateMachine _stateMachine;

    // Presentation point: rental rules live in one Service Layer class rather than
    // being duplicated between API endpoints and mobile ViewModels.
    public RentalWorkflowService(
        IRentalRepository rentals,
        IItemRepository items,
        IUnitOfWork unitOfWork,
        RentalStateMachine stateMachine)
    {
        _rentals = rentals;
        _items = items;
        _unitOfWork = unitOfWork;
        _stateMachine = stateMachine;
    }

    public async Task<RentalSummaryDto> RequestAsync(
        Guid borrowerId,
        CreateRentalRequest request,
        CancellationToken cancellationToken = default)
    {
        var start = request.StartDateUtc.ToUniversalTime();
        var end = request.EndDateUtc.ToUniversalTime();
        if (start.Date < DateTimeOffset.UtcNow.Date || end.Date < start.Date)
        {
            throw new BusinessRuleException("Rental dates must start today or later, and the end date cannot precede the start date.");
        }

        var item = await _items.GetDetailsAsync(request.ItemId, cancellationToken)
            ?? throw new KeyNotFoundException("Item not found.");
        if (!item.IsAvailable)
        {
            throw new BusinessRuleException("This item is not currently available.");
        }

        if (item.OwnerId == borrowerId)
        {
            throw new BusinessRuleException("You cannot rent your own item.");
        }

        if (await _rentals.HasDateOverlapAsync(item.Id, start, end, cancellationToken))
        {
            throw new BusinessRuleException("The item is already booked for some or all of those dates.");
        }

        // Inclusive charging means a Monday-to-Tuesday rental is two billable days.
        var numberOfDays = (end.Date - start.Date).Days + 1;
        var rental = new Rental
        {
            ItemId = item.Id,
            BorrowerId = borrowerId,
            StartDateUtc = start,
            EndDateUtc = end,
            TotalPrice = item.DailyRate * numberOfDays
        };
        await _rentals.AddAsync(rental, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var savedRental = await _rentals.GetDetailsAsync(rental.Id, cancellationToken)
            ?? throw new InvalidOperationException("The saved rental could not be reloaded.");
        return savedRental.ToSummary();
    }

    public async Task<IReadOnlyList<RentalSummaryDto>> GetIncomingAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var incomingRentals = await _rentals.GetIncomingAsync(ownerId, cancellationToken);
        return incomingRentals.Select(rental => rental.ToSummary()).ToList();
    }

    public async Task<IReadOnlyList<RentalSummaryDto>> GetOutgoingAsync(
        Guid borrowerId,
        CancellationToken cancellationToken = default)
    {
        var outgoingRentals = await _rentals.GetOutgoingAsync(borrowerId, cancellationToken);
        return outgoingRentals.Select(rental => rental.ToSummary()).ToList();
    }

    public async Task<RentalSummaryDto> TransitionAsync(
        Guid userId,
        Guid rentalId,
        RentalStatus nextStatus,
        CancellationToken cancellationToken = default)
    {
        var rental = await _rentals.GetDetailsAsync(rentalId, cancellationToken)
            ?? throw new KeyNotFoundException("Rental not found.");
        // Role permission and state validity are separate responsibilities: the
        // service checks the actor, then the State Pattern checks the transition.
        EnsureActorCanTransition(rental, userId, nextStatus);
        _stateMachine.EnsureValidTransition(rental.Status, nextStatus);

        rental.Status = nextStatus;
        rental.UpdatedAtUtc = DateTimeOffset.UtcNow;
        _rentals.Update(rental);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return rental.ToSummary();
    }

    public async Task<int> MarkOverdueAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        // The background worker calls the same state machine as user-driven actions,
        // avoiding a privileged shortcut around the domain workflow.
        var candidates = await _rentals.GetOverdueCandidatesAsync(now, cancellationToken);
        foreach (var rental in candidates)
        {
            _stateMachine.EnsureValidTransition(rental.Status, RentalStatus.Overdue);
            rental.Status = RentalStatus.Overdue;
            rental.UpdatedAtUtc = now;
            _rentals.Update(rental);
        }

        if (candidates.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return candidates.Count;
    }

    private static void EnsureActorCanTransition(Rental rental, Guid userId, RentalStatus nextStatus)
    {
        // Owners manage approval/dispatch/completion; borrowers acknowledge return.
        var isOwner = rental.Item.OwnerId == userId;
        var isBorrower = rental.BorrowerId == userId;

        bool permitted;
        if (nextStatus == RentalStatus.Approved ||
            nextStatus == RentalStatus.Rejected ||
            nextStatus == RentalStatus.OutForRent ||
            nextStatus == RentalStatus.Completed)
        {
            permitted = isOwner;
        }
        else if (nextStatus == RentalStatus.Returned)
        {
            permitted = isBorrower;
        }
        else
        {
            permitted = false;
        }

        if (!permitted)
        {
            throw new UnauthorizedAccessException("You are not allowed to perform that rental transition.");
        }
    }
}

internal static class RentalMappingExtensions
{
    public static RentalSummaryDto ToSummary(this Rental rental)
    {
        return new RentalSummaryDto(
            rental.Id,
            rental.ItemId,
            rental.Item.Title,
            rental.Item.OwnerId,
            rental.Item.Owner.DisplayName,
            rental.BorrowerId,
            rental.Borrower.DisplayName,
            rental.StartDateUtc,
            rental.EndDateUtc,
            rental.TotalPrice,
            rental.Status,
            rental.CreatedAtUtc);
    }
}
