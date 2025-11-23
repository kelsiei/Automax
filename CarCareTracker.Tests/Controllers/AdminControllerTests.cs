using System.Security.Claims;
using CarCareTracker.Controllers;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarCareTracker.Tests.Controllers;

public class AdminControllerTests
{
    [Fact]
    public async Task Index_NonRoot_Forbid()
    {
        var controller = BuildController();
        controller.ControllerContext = BuildContext(isRootUser: false);

        var result = await controller.Index();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Index_Root_ReturnsViewWithUsers()
    {
        var (controller, userDataAccess, _) = BuildControllerWithMocks();
        controller.ControllerContext = BuildContext(isRootUser: true);

        userDataAccess.Setup(u => u.GetAllUsersAsync()).ReturnsAsync(new List<UserData>
        {
            new() { Id = 1, UserName = "root", IsRootUser = true },
            new() { Id = 2, UserName = "admin", IsRootUser = false }
        });

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<UserData>>(view.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public async Task Delete_RootUser_NotAllowed()
    {
        var (controller, userDataAccess, _) = BuildControllerWithMocks();
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.ControllerContext = BuildContext(isRootUser: true);

        userDataAccess.Setup(u => u.GetUserByIdAsync(1)).ReturnsAsync(new UserData { Id = 1, IsRootUser = true, UserName = "root" });

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), redirect.ActionName);
        userDataAccess.Verify(u => u.DeleteUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_NonRootUser_Deletes()
    {
        var (controller, userDataAccess, _) = BuildControllerWithMocks();
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        controller.ControllerContext = BuildContext(isRootUser: true);

        userDataAccess.Setup(u => u.GetUserByIdAsync(2)).ReturnsAsync(new UserData { Id = 2, IsRootUser = false, UserName = "user" });

        var result = await controller.Delete(2);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), redirect.ActionName);
        userDataAccess.Verify(u => u.DeleteUserAsync(2), Times.Once);
    }

    private (AdminController controller, Mock<IUserRecordDataAccess> userDataAccess, Mock<IPasswordHelper> passwordHelper) BuildControllerWithMocks()
    {
        var logger = Mock.Of<ILogger<AdminController>>();
        var userDataAccess = new Mock<IUserRecordDataAccess>();
        var passwordHelper = new Mock<IPasswordHelper>();

        var controller = new AdminController(logger, userDataAccess.Object, passwordHelper.Object);
        return (controller, userDataAccess, passwordHelper);
    }

    private AdminController BuildController()
    {
        var logger = Mock.Of<ILogger<AdminController>>();
        var userDataAccess = Mock.Of<IUserRecordDataAccess>();
        var passwordHelper = Mock.Of<IPasswordHelper>();
        return new AdminController(logger, userDataAccess, passwordHelper);
    }

    private ControllerContext BuildContext(bool isRootUser)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        if (isRootUser)
        {
            claims.Add(new Claim("IsRootUser", "true"));
        }
        else
        {
            claims.Add(new Claim("IsRootUser", "false"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
