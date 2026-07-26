using RentalApp.Application.ViewModels;

namespace RentalApp.Views;

public partial class RentalsPage : ContentPage
{
    private readonly RentalsViewModel _viewModel;

    public RentalsPage(RentalsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadCommand.Execute(null);
    }
}
