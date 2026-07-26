using RentalApp.Application.Services;

namespace RentalApp.Services;

public sealed class MauiNavigationService : INavigationService
{
    public Task GoToAsync(string route, IReadOnlyDictionary<string, object>? parameters = null) =>
        parameters is null
            ? Shell.Current.GoToAsync(route)
            : Shell.Current.GoToAsync(
                route,
                new ShellNavigationQueryParameters(new Dictionary<string, object>(parameters)));
}
