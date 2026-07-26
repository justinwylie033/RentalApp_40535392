using RentalApp.Database.Models;

namespace RentalApp.Database.Data.Repositories;

/// <summary>Provides verified-review queries and rating aggregates.</summary>
public interface IReviewRepository : IRepository<Review>
{
    /// <summary>Returns all reviews for one item.</summary>
    Task<IReadOnlyList<Review>> GetForItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    /// <summary>Checks whether the rental already has its one permitted review.</summary>
    Task<bool> ExistsForRentalAsync(Guid rentalId, CancellationToken cancellationToken = default);
    /// <summary>Returns the average rating and review count for a listing creator.</summary>
    Task<(double Average, int Count)> GetUserRatingAsync(Guid userId, CancellationToken cancellationToken = default);
}
