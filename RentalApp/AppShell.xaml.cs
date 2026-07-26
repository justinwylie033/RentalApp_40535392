using RentalApp.Application.Services;
using RentalApp.Views;

namespace RentalApp;

public partial class AppShell : Shell
{
    private readonly IAuthenticationService _authentication;

    public AppShell(
        IAuthenticationService authentication,
        LoginPage loginPage,
        ItemsListPage itemsPage,
        MyListingsPage myListingsPage,
        NearbyItemsPage nearbyPage,
        CreateItemPage createPage,
        RentalsPage rentalsPage,
        ProfilePage profilePage)
    {
        InitializeComponent();
        _authentication = authentication;
        Items.Add(new ShellContent { Title = "Sign in", Route = "login", Content = loginPage });
        Items.Add(CreateFlyoutItem("Browse", "items", "Items", itemsPage));
        Items.Add(CreateFlyoutItem("My listings", "my-listings", "My listings", myListingsPage));
        Items.Add(CreateFlyoutItem("Nearby", "nearby", "Near me", nearbyPage));
        Items.Add(CreateFlyoutItem("List an item", "create", "Create", createPage));
        Items.Add(CreateFlyoutItem("Rentals", "rentals", "Rentals", rentalsPage));
        Items.Add(CreateFlyoutItem("Profile", "profile", "Profile", profilePage));
        Routing.RegisterRoute("item-detail", typeof(ItemDetailPage));
        Routing.RegisterRoute("reviews", typeof(ReviewsPage));
    }

    private async void OnSignOutClicked(object? sender, EventArgs eventArgs)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
        }

        try
        {
            // Clear Secure Storage before returning to the login route so another
            // person cannot inherit the previous account's authenticated session.
            await _authentication.LogoutAsync();
            FlyoutIsPresented = false;
            await GoToAsync(AppRoutes.Login);
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Sign out failed", exception.Message, "OK");
        }
        finally
        {
            if (sender is Button signOutButton)
            {
                signOutButton.IsEnabled = true;
            }
        }
    }

    private static FlyoutItem CreateFlyoutItem(
        string title,
        string route,
        string contentTitle,
        Page page)
    {
        var item = new FlyoutItem { Title = title, Route = route };
        item.Items.Add(new ShellContent { Title = contentTitle, Content = page });
        return item;
    }
}
