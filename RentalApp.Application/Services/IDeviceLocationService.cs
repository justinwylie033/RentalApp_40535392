namespace RentalApp.Application.Services;

public sealed record GeoCoordinate(double Latitude, double Longitude);

/// <summary>Provides the device position without exposing MAUI APIs to ViewModels.</summary>
public interface IDeviceLocationService
{
    /// <summary>Returns the current device latitude and longitude.</summary>
    Task<GeoCoordinate> GetCurrentAsync(CancellationToken cancellationToken = default);
}
