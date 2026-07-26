namespace RentalApp.Contracts;

/// <summary>
/// Carries one deterministic page plus enough metadata for clients to request
/// subsequent pages without guessing whether more records exist.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;
}
