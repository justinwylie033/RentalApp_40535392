using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalApp.Application.Services;
using RentalApp.Contracts;

namespace RentalApp.Application.ViewModels;

public partial class ProfileViewModel(
    IAuthenticationService authentication,
    INavigationService navigation) : ViewModelBase
{
    [ObservableProperty]
    private UserProfileDto? profile;

    [RelayCommand]
    private Task LoadAsync() => RunBusyAsync(async () => Profile = await authentication.GetProfileAsync());

    [RelayCommand]
    private Task LogoutAsync() => RunBusyAsync(async () =>
    {
        await authentication.LogoutAsync();
        Profile = null;
        await navigation.GoToAsync(AppRoutes.Login);
    });
}
