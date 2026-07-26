namespace RentalApp.Application.Services;

/// <summary>Abstracts Shell navigation for testable ViewModel commands.</summary>
public interface INavigationService
{
    /// <summary>Navigates to a route with optional typed route parameters.</summary>
    Task GoToAsync(string route, IReadOnlyDictionary<string, object>? parameters = null);
}

public static class AppRoutes
{
    public const string Login = "//login";
    public const string Items = "//items";
    public const string ItemDetail = "item-detail";
    public const string Reviews = "reviews";
}
