using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;

namespace RentalApp.Application.ViewModels;

public partial class LoginViewModel(
    IAuthenticationService authentication,
    INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private string displayName = string.Empty;

    [ObservableProperty]
    private string email = "mike@example.com";

    [ObservableProperty]
    private string password = "Rental123!";

    [RelayCommand]
    private void UseDemoAccount()
    {
        DisplayName = string.Empty;
        Email = "mike@example.com";
        Password = "Rental123!";
    }

    [RelayCommand]
    private Task LoginAsync() => RunBusyAsync(async () =>
    {
        await authentication.LoginAsync(Email, Password);
        await navigation.GoToAsync(AppRoutes.Items);
    });

    [RelayCommand]
    private Task RegisterAsync() => RunBusyAsync(async () =>
    {
        await authentication.RegisterAsync(DisplayName, Email, Password);
        await navigation.GoToAsync(AppRoutes.Items);
    });
}
