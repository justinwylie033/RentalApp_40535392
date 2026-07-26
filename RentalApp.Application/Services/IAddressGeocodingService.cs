namespace RentalApp.Application.Services;

public sealed record ResolvedAddress(string DisplayAddress, double Latitude, double Longitude);

/// <summary>Hides platform geocoding so address workflows remain testable.</summary>
public interface IAddressGeocodingService
{
    /// <summary>Converts a user-entered address into display text and coordinates.</summary>
    Task<ResolvedAddress> ResolveAsync(
        string address,
        CancellationToken cancellationToken = default);

    /// <summary>Converts device coordinates into a readable collection address.</summary>
    Task<ResolvedAddress> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
