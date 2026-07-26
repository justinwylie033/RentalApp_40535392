using System.Globalization;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Services;
using RentalApp.Views;

namespace RentalApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Use familiar UK dates and number formatting throughout the mobile UI.
        // API timestamps remain strongly typed UTC values and JSON stays ISO 8601.
        var ukCulture = CultureInfo.GetCultureInfo("en-GB");
        CultureInfo.CurrentCulture = ukCulture;
        CultureInfo.CurrentUICulture = ukCulture;
        CultureInfo.DefaultThreadCurrentCulture = ukCulture;
        CultureInfo.DefaultThreadCurrentUICulture = ukCulture;

        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // Presentation point: this is the mobile composition root. Views depend on
        // ViewModels, and ViewModels depend on interfaces rather than platform types.
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddSingleton<INavigationService, MauiNavigationService>();
        builder.Services.AddSingleton<IDeviceLocationService, DeviceLocationService>();
        builder.Services.AddSingleton<IAddressGeocodingService, AddressGeocodingService>();
        builder.Services.AddSingleton<IApiClient>(provider => new ApiClient(
            new HttpClient
            {
                BaseAddress = new Uri("http://10.0.2.2:8080/"),
                Timeout = TimeSpan.FromSeconds(15)
            },
            provider.GetRequiredService<ITokenStore>()));
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<IItemService, ItemService>();
        builder.Services.AddSingleton<IRentalService, RentalService>();
        builder.Services.AddSingleton<IReviewService, ReviewService>();

        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<ItemsListViewModel>();
        builder.Services.AddTransient<ItemDetailViewModel>();
        builder.Services.AddTransient<CreateItemViewModel>();
        builder.Services.AddTransient<NearbyItemsViewModel>();
        builder.Services.AddTransient<RentalsViewModel>();
        builder.Services.AddTransient<ReviewsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<ItemsListPage>();
        builder.Services.AddTransient<ItemDetailPage>();
        builder.Services.AddTransient<CreateItemPage>();
        builder.Services.AddTransient<NearbyItemsPage>();
        builder.Services.AddTransient<RentalsPage>();
        builder.Services.AddTransient<ReviewsPage>();
        builder.Services.AddTransient<ProfilePage>();

        return builder.Build();
    }
}
