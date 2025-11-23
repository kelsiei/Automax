using System.Security.Claims;
using CarCareTracker.Controllers;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.Login;
using CarCareTracker.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarCareTracker.Tests.Controllers;

public class LoginControllerTests
{
    [Fact]
    public async Task Index_Post_InvalidCredentials_ReturnsViewWithError()
    {
        var (controller, userData, passwordHelper) = BuildControllerWithMocks();
        controller.ControllerContext = BuildContext();

        userData.Setup(u => u.GetUserByUserNameAsync("user")).ReturnsAsync(new UserData { UserName = "user", PasswordHash = "hash" });
        passwordHelper.Setup(p => p.VerifyPassword("wrong", "hash")).Returns(false);

        var model = new LoginModel { UserName = "user", Password = "wrong" };

        var result = await controller.Index(model, null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ErrorCount > 0);
    }

    [Fact]
    public async Task Index_Post_ValidCredentials_RedirectsToHome()
    {
        var (controller, userData, passwordHelper) = BuildControllerWithMocks();
        controller.ControllerContext = BuildContext();

        userData.Setup(u => u.GetUserByUserNameAsync("user")).ReturnsAsync(new UserData { Id = 1, UserName = "user", PasswordHash = "hash", EmailAddress = "e@example.com", IsAdmin = false, IsRootUser = false });
        passwordHelper.Setup(p => p.VerifyPassword("correct", "hash")).Returns(true);

        var model = new LoginModel { UserName = "user", Password = "correct" };

        var result = await controller.Index(model, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task Index_Get_SeedsRootWhenNoUsers()
    {
        var (controller, userData, passwordHelper) = BuildControllerWithMocks();
        controller.ControllerContext = BuildContext();

        userData.Setup(u => u.AnyUsersAsync()).ReturnsAsync(false);
        userData.Setup(u => u.SaveUserAsync(It.Is<UserData>(d => d.IsRootUser && d.IsAdmin && d.UserName == "root"))).Returns(Task.CompletedTask).Verifiable();

        var result = await controller.Index(null);

        Assert.IsType<ViewResult>(result);
        userData.Verify();
    }

    [Fact]
    public async Task Index_Get_DoesNotSeedWhenUsersExist()
    {
        var (controller, userData, _) = BuildControllerWithMocks();
        controller.ControllerContext = BuildContext();

        userData.Setup(u => u.AnyUsersAsync()).ReturnsAsync(true);

        var result = await controller.Index(null);

        Assert.IsType<ViewResult>(result);
        userData.Verify(u => u.SaveUserAsync(It.IsAny<UserData>()), Times.Never);
    }

    private (LoginController controller, Mock<IUserRecordDataAccess> userData, Mock<IPasswordHelper> passwordHelper) BuildControllerWithMocks()
    {
        var logger = Mock.Of<ILogger<LoginController>>();
        var userData = new Mock<IUserRecordDataAccess>();
        var passwordHelper = new Mock<IPasswordHelper>();
        var controller = new LoginController(logger, userData.Object, passwordHelper.Object);
        return (controller, userData, passwordHelper);
    }

    private ControllerContext BuildContext()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>(), "Test"));
        return new ControllerContext { HttpContext = context };
    }
}
