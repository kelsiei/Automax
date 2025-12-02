using System.Security.Claims;
using Automax.Controllers;
using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Models.Admin;
using Automax.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Automax.Tests.Controllers;

public class AdminControllerTests
{
    [Fact]
    public async Task Edit_AllowsAdminToUpdatePassword_WhenNewPasswordProvided()
    {
        // Arrange
        var passwordHelper = new PasswordHelper();
        var originalHash = passwordHelper.HashPassword("OldPass123");
        var user = new UserData
        {
            Id = 42,
            UserName = "regularuser",
            EmailAddress = "regular@example.com",
            PasswordHash = originalHash,
            IsAdmin = false,
            IsRootUser = false
        };

        UserData savedUser = null!;

        var userDataAccess = new Mock<IUserRecordDataAccess>();
        userDataAccess.Setup(x => x.GetUserByIdAsync(user.Id)).ReturnsAsync(user);
        userDataAccess.Setup(x => x.SaveUserAsync(It.IsAny<UserData>()))
            .Callback<UserData>(u => savedUser = u)
            .ReturnsAsync(user.Id);

        var logger = new Mock<ILogger<AdminController>>();

        var controller = new AdminController(logger.Object, userDataAccess.Object, passwordHelper)
        {
            ControllerContext = BuildRootUserContext()
        };
        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext,
            Mock.Of<ITempDataProvider>());

        var model = new AdminUserEditModel
        {
            Id = user.Id,
            UserName = user.UserName,
            EmailAddress = user.EmailAddress,
            IsAdmin = user.IsAdmin,
            IsRootUser = user.IsRootUser,
            NewPassword = "NewPass456",
            ConfirmNewPassword = "NewPass456"
        };

        // Act
        var result = await controller.Edit(model);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminController.Index), redirectResult.ActionName);

        Assert.NotNull(savedUser);
        Assert.NotEqual(originalHash, savedUser!.PasswordHash);
        Assert.True(passwordHelper.VerifyPassword("NewPass456", savedUser.PasswordHash));
    }

    private static ControllerContext BuildRootUserContext()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new("IsRootUser", "true")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = principal
            }
        };
    }
}
