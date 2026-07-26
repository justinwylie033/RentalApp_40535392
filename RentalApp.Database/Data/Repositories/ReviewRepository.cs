using Microsoft.EntityFrameworkCore;
using RentalApp.Database.Models;

namespace RentalApp.Database.Data.Repositories;

public sealed class ReviewRepository(AppDbContext context) : Repository<Review>(context), IReviewRepository
{
    public async Task<IReadOnlyList<Review>> GetForItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default) =>
        await Context.Reviews
            .AsNoTracking()
            .Include(review => review.Reviewer)
            .Where(review => review.ItemId == itemId)
            .OrderByDescending(review => review.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForRentalAsync(Guid rentalId, CancellationToken cancellationToken = default) =>
        Context.Reviews.AnyAsync(review => review.RentalId == rentalId, cancellationToken);

    public async Task<(double Average, int Count)> GetUserRatingAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Reviews.Where(review => review.Item.OwnerId == userId);
        var count = await query.CountAsync(cancellationToken);
        var average = count == 0 ? 0 : await query.AverageAsync(review => review.Rating, cancellationToken);
        return (average, count);
    }
}
