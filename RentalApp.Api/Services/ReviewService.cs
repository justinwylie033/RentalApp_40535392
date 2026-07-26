using RentalApp.Contracts;
using RentalApp.Database.Data.Repositories;
using RentalApp.Database.Models;

namespace RentalApp.Api.Services;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetForItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<ReviewDto> CreateAsync(Guid reviewerId, CreateReviewRequest request, CancellationToken cancellationToken = default);
}

public sealed class ReviewService(
    IReviewRepository reviews,
    IRentalRepository rentals,
    IUnitOfWork unitOfWork) : IReviewService
{
    // Presentation point: reviews are verified transactions, not free-form comments.
    // The borrower, completed state, and one-review-per-rental rule are all enforced.
    public async Task<IReadOnlyList<ReviewDto>> GetForItemAsync(
        Guid itemId,
        CancellationToken cancellationToken = default) =>
        (await reviews.GetForItemAsync(itemId, cancellationToken)).Select(review => review.ToDto()).ToList();

    public async Task<ReviewDto> CreateAsync(
        Guid reviewerId,
        CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
        {
            throw new BusinessRuleException("Rating must be between 1 and 5.");
        }

        if (request.Comment.Trim().Length is < 3 or > 1_000)
        {
            throw new BusinessRuleException("Comment must contain between 3 and 1,000 characters.");
        }

        var rental = await rentals.GetDetailsAsync(request.RentalId, cancellationToken)
            ?? throw new KeyNotFoundException("Rental not found.");
        if (rental.BorrowerId != reviewerId)
        {
            throw new UnauthorizedAccessException("Only the borrower can review this rental.");
        }

        if (rental.Status != RentalStatus.Completed)
        {
            throw new BusinessRuleException("A review can only be submitted after the rental is completed.");
        }

        if (await reviews.ExistsForRentalAsync(rental.Id, cancellationToken))
        {
            throw new BusinessRuleException("A review has already been submitted for this rental.");
        }

        var review = new Review
        {
            RentalId = rental.Id,
            ItemId = rental.ItemId,
            ReviewerId = reviewerId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            Reviewer = rental.Borrower
        };
        await reviews.AddAsync(review, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return review.ToDto();
    }
}

internal static class ReviewMappingExtensions
{
    public static ReviewDto ToDto(this Review review) => new(
        review.Id,
        review.RentalId,
        review.ItemId,
        review.ReviewerId,
        review.Reviewer.DisplayName,
        review.Rating,
        review.Comment,
        review.CreatedAtUtc);
}
