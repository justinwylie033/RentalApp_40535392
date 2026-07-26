using RentalApp.Application.ViewModels;

namespace RentalApp.Views;

public partial class ItemDetailPage : ContentPage, IQueryAttributable
{
    private readonly ItemDetailViewModel _viewModel;

    public ItemDetailPage(ItemDetailViewModel viewModel)
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

        var itemId = value switch
        {
            Guid id => id,
            string text when Guid.TryParse(text, out var id) => id,
            _ => Guid.Empty
        };

        if (itemId != Guid.Empty)
        {
            _ = _viewModel.LoadAsync(itemId);
        }
    }
}
