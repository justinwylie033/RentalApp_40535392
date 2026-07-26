using Microsoft.EntityFrameworkCore;
using RentalApp.Contracts;
using RentalApp.Database.Models;

namespace RentalApp.Database.Data.Repositories;

public sealed class RentalRepository(AppDbContext context) : Repository<Rental>(context), IRentalRepository
{
    public Task<Rental?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Context.Rentals
            .Include(rental => rental.Item)
                .ThenInclude(item => item.Owner)
            .Include(rental => rental.Borrower)
            .Include(rental => rental.Review)
            .SingleOrDefaultAsync(rental => rental.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Rental>> GetIncomingAsync(
        Guid ownerId,
        CancellationToken cancellationToken = default) =>
        await Context.Rentals
            .AsNoTracking()
            .Include(rental => rental.Item)
                .ThenInclude(item => item.Owner)
            .Include(rental => rental.Borrower)
            .Where(rental => rental.Item.OwnerId == ownerId)
            .OrderByDescending(rental => rental.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Rental>> GetOutgoingAsync(
        Guid borrowerId,
        CancellationToken cancellationToken = default) =>
        await Context.Rentals
            .AsNoTracking()
            .Include(rental => rental.Item)
                .ThenInclude(item => item.Owner)
            .Include(rental => rental.Borrower)
            .Where(rental => rental.BorrowerId == borrowerId)
            .OrderByDescending(rental => rental.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> HasDateOverlapAsync(
        Guid itemId,
        DateTimeOffset startDateUtc,
        DateTimeOffset endDateUtc,
        CancellationToken cancellationToken = default) =>
        Context.Rentals.AnyAsync(
            rental => rental.ItemId == itemId
                && rental.Status != RentalStatus.Rejected
                && rental.Status != RentalStatus.Cancelled
                && startDateUtc <= rental.EndDateUtc
                && endDateUtc >= rental.StartDateUtc,
            cancellationToken);

    public async Task<IReadOnlyList<Rental>> GetOverdueCandidatesAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        await Context.Rentals
            .Where(rental => rental.Status == RentalStatus.OutForRent && rental.EndDateUtc < now)
            .ToListAsync(cancellationToken);
}
