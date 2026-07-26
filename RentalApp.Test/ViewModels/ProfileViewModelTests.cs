using Moq;
using RentalApp.Application.Services;
using RentalApp.Application.ViewModels;
using RentalApp.Contracts;

namespace RentalApp.Test.ViewModels;

public sealed class ProfileViewModelTests
{
    [Fact]
    public async Task LoadCommand_AuthenticatedUser_PopulatesProfile()
    {
        var profile = new UserProfileDto(Guid.NewGuid(), "Mike", "mike@example.com", 4.5, 2);
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.GetProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var viewModel = new ProfileViewModel(authentication.Object, Mock.Of<INavigationService>());

        await viewModel.LoadCommand.ExecuteAsync(null);

        Assert.Equal(profile, viewModel.Profile);
        Assert.Null(viewModel.ErrorMessage);
    }

    [Fact]
    public async Task LogoutCommand_AuthenticatedUser_ClearsProfileAndNavigatesToLogin()
    {
        var authentication = new Mock<IAuthenticationService>();
        authentication.Setup(service => service.LogoutAsync()).Returns(Task.CompletedTask);
        var navigation = new Mock<INavigationService>();
        navigation.Setup(service => service.GoToAsync(AppRoutes.Login, null)).Returns(Task.CompletedTask);
        var viewModel = new ProfileViewModel(authentication.Object, navigation.Object)
        {
            Profile = new UserProfileDto(Guid.NewGuid(), "Mike", "mike@example.com", 0, 0)
        };

        await viewModel.LogoutCommand.ExecuteAsync(null);

        Assert.Null(viewModel.Profile);
        authentication.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(AppRoutes.Login, null), Times.Once);
    }
}
