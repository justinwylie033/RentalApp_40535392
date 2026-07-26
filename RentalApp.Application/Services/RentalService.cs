using RentalApp.Contracts;

namespace RentalApp.Application.Services;

public interface IRentalService
{
    Task<RentalSummaryDto> RequestAsync(CreateRentalRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RentalSummaryDto>> GetIncomingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RentalSummaryDto>> GetOutgoingAsync(CancellationToken cancellationToken = default);
    Task<RentalSummaryDto> UpdateStatusAsync(Guid rentalId, RentalStatus status, CancellationToken cancellationToken = default);
}

public sealed class RentalService(IApiClient api) : IRentalService
{
    public Task<RentalSummaryDto> RequestAsync(
        CreateRentalRequest request,
        CancellationToken cancellationToken = default) =>
        api.PostAsync<CreateRentalRequest, RentalSummaryDto>("rentals/", request, cancellationToken);

    public Task<IReadOnlyList<RentalSummaryDto>> GetIncomingAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<IReadOnlyList<RentalSummaryDto>>("rentals/incoming", cancellationToken);

    public Task<IReadOnlyList<RentalSummaryDto>> GetOutgoingAsync(CancellationToken cancellationToken = default) =>
        api.GetAsync<IReadOnlyList<RentalSummaryDto>>("rentals/outgoing", cancellationToken);

    public Task<RentalSummaryDto> UpdateStatusAsync(
        Guid rentalId,
        RentalStatus status,
        CancellationToken cancellationToken = default) =>
        api.PatchAsync<UpdateRentalStatusRequest, RentalSummaryDto>(
            $"rentals/{rentalId}/status",
            new UpdateRentalStatusRequest(status),
            cancellationToken);
}
