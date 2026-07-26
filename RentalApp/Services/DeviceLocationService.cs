using RentalApp.Application.Services;

namespace RentalApp.Services;

public sealed class DeviceLocationService : IDeviceLocationService
{
    // Presentation point: platform permission and sensor APIs are hidden behind an
    // application interface so nearby-search ViewModels can use mocked coordinates.
    public async Task<GeoCoordinate> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (permission != PermissionStatus.Granted)
        {
            throw new InvalidOperationException("Location permission is required for nearby search.");
        }

        var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
        var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken)
            ?? throw new InvalidOperationException("The device could not determine its current location.");
        return new GeoCoordinate(location.Latitude, location.Longitude);
    }
}
