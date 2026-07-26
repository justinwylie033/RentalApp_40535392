using RentalApp.Contracts;

namespace RentalApp.Application.Services;

public interface IReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetForItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken cancellationToken = default);
}

public sealed class ReviewService(IApiClient api) : IReviewService
{
    public Task<IReadOnlyList<ReviewDto>> GetForItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        api.GetAsync<IReadOnlyList<ReviewDto>>($"reviews/items/{itemId}", cancellationToken);

    public Task<ReviewDto> CreateAsync(CreateReviewRequest request, CancellationToken cancellationToken = default) =>
        api.PostAsync<CreateReviewRequest, ReviewDto>("reviews/", request, cancellationToken);
}
