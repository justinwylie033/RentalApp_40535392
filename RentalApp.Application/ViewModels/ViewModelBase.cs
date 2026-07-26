using CommunityToolkit.Mvvm.ComponentModel;

namespace RentalApp.Application.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    // Presentation point: all ViewModels share busy-state and error handling. This
    // keeps page code-behind minimal and prevents duplicate command execution.
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    protected async Task RunBusyAsync(Func<Task> operation)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;
            await operation();
        }
        catch (Exception exception)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
