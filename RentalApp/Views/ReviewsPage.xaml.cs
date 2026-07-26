using RentalApp.Application.ViewModels;

namespace RentalApp.Views;

public partial class ReviewsPage : ContentPage, IQueryAttributable
{
    private readonly ReviewsViewModel _viewModel;

    public ReviewsPage(ReviewsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("itemId", out var value))
        {
            return;
        }

        var itemId = ReadGuid(value);
        var rentalId = query.TryGetValue("rentalId", out var rentalValue)
            ? ReadGuid(rentalValue)
            : Guid.Empty;

        if (itemId != Guid.Empty)
        {
            _ = _viewModel.LoadAsync(
                itemId,
                rentalId == Guid.Empty ? null : rentalId);
        }
    }

    private static Guid ReadGuid(object value) => value switch
    {
        Guid id => id,
        string text when Guid.TryParse(text, out var id) => id,
        _ => Guid.Empty
    };
}
