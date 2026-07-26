using RentalApp.Application.ViewModels;

namespace RentalApp.Views;

public partial class MyListingsPage : ContentPage
{
    private readonly MyListingsViewModel _viewModel;

    public MyListingsPage(MyListingsViewModel viewModel)
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
