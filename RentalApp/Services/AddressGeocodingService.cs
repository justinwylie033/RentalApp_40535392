using RentalApp.Application.Services;

namespace RentalApp.Services;

public sealed class AddressGeocodingService : IAddressGeocodingService
{
    // Presentation point: this MAUI adapter isolates platform geocoding from the
    // application library, leaving ViewModels mockable in ordinary xUnit tests.
    public async Task<ResolvedAddress> ResolveAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = address.Trim();
        if (query.Length < 5)
        {
            throw new InvalidOperationException("Enter a complete collection address, including town or postcode.");
        }

        try
        {
            // Forward geocoding converts a human address into a WGS84 coordinate.
            var location = (await Geocoding.Default.GetLocationsAsync(query)).FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "That address could not be found. Include the street, town and postcode.");
            cancellationToken.ThrowIfCancellationRequested();

            var displayAddress = await TryGetDisplayAddressAsync(location.Latitude, location.Longitude)
                ?? query;
            return new ResolvedAddress(displayAddress, location.Latitude, location.Longitude);
        }
        catch (FeatureNotSupportedException exception)
        {
            throw new InvalidOperationException(
                "Address lookup is not supported on this device. Use current location instead.",
                exception);
        }
    }

    public async Task<ResolvedAddress> ReverseAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        // Reverse geocoding turns a GPS coordinate back into a readable address.
        cancellationToken.ThrowIfCancellationRequested();
        var displayAddress = await TryGetDisplayAddressAsync(latitude, longitude)
            ?? $"Current location ({latitude:F5}, {longitude:F5})";
        return new ResolvedAddress(displayAddress, latitude, longitude);
    }

    private static async Task<string?> TryGetDisplayAddressAsync(double latitude, double longitude)
    {
        var placemark = (await Geocoding.Default.GetPlacemarksAsync(latitude, longitude)).FirstOrDefault();
        if (placemark is null)
        {
            return null;
        }

        // Placemark fields differ by country, so empty components are removed before
        // producing a compact address suitable for display and persistence.
        var street = string.Join(
            " ",
            new[] { placemark.SubThoroughfare, placemark.Thoroughfare }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
        var parts = new[]
        {
            street,
            placemark.Locality,
            placemark.AdminArea,
            placemark.PostalCode,
            placemark.CountryName
        };
        var formatted = string.Join(
            ", ",
            parts.Where(part => !string.IsNullOrWhiteSpace(part)).Distinct(StringComparer.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }
}
