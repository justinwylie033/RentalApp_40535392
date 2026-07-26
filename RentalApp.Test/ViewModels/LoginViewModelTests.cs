using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class LoginViewModelTests
{
    [Fact]
    public async Task LoginCommand_ValidCredentials_NavigatesToItems()
    {
        var profile = new UserProfileDto(Guid.NewGuid(), "Mike", "mike@example.com", 0, 0);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.LoginAsync("mike@example.com", "Rental123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var navigation = new Mock<INavigationService>();
        var viewModel = new LoginViewModel(authentication.Object, navigation.Object);

        await viewModel.LoginCommand.ExecuteAsync(null);

        navigation.Verify(service => service.GoToAsync(AppRoutes.Items, null), Times.Once);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task RegisterCommand_ApiRejectsRegistration_ShowsReason()
    {
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.RegisterAsync(
                "Mike", "mike@example.com", "weak", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Password is too weak."));
        var viewModel = new LoginViewModel(authentication.Object, Mock.Of<INavigationService>())
        {
            DisplayName = "Mike",
            Password = "weak"
        };

        await viewModel.RegisterCommand.ExecuteAsync(null);

        Assert.Equal("Password is too weak.", viewModel.ErrorMessage);
    }
}
