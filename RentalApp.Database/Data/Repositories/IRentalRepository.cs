using RentalApp.Database.Models;

namespace RentalApp.Database.Data.Repositories;

/// <summary>Provides rental workflow and booking-conflict queries.</summary>
public interface IRentalRepository : IRepository<Rental>
{
    /// <summary>Returns a rental with its listing and user relationships.</summary>
    Task<Rental?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Returns requests for listings created by the supplied user.</summary>
    Task<IReadOnlyList<Rental>> GetIncomingAsync(Guid ownerId, CancellationToken cancellationToken = default);
    /// <summary>Returns rentals requested by the supplied user.</summary>
    Task<IReadOnlyList<Rental>> GetOutgoingAsync(Guid borrowerId, CancellationToken cancellationToken = default);
    /// <summary>Checks inclusive date overlap against active bookings.</summary>
    Task<bool> HasDateOverlapAsync(
        Guid itemId,
        DateTimeOffset startDateUtc,
        DateTimeOffset endDateUtc,
        CancellationToken cancellationToken = default);
    /// <summary>Returns out-for-rent records whose due date has passed.</summary>
    Task<IReadOnlyList<Rental>> GetOverdueCandidatesAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
