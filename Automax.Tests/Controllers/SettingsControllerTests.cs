using System.Security.Claims;
using Automax.Controllers;
using Automax.Helper;
using Automax.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Automax.Tests.Controllers;

public class SettingsControllerTests
{
    [Fact]
    public void Index_NonRootUser_Forbid()
    {
        var configHelper = BuildConfigHelperMock();
        var controller = BuildController(configHelper);
        controller.ControllerContext = BuildContext(isRootUser: false);

        var result = controller.Index();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void Index_RootUser_ReturnsView()
    {
        var configHelper = BuildConfigHelperMock();
        configHelper.Setup(c => c.LoadServerConfig()).Returns(new ServerConfig());
        var controller = BuildController(configHelper);
        controller.ControllerContext = BuildContext(isRootUser: true);

        var result = controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ServerSettingsViewModel>(view.Model);
    }

    [Fact]
    public void Index_Post_Success_RedirectsAndSetsTempData()
    {
        var configHelper = BuildConfigHelperMock();
        configHelper.Setup(c => c.LoadServerConfig()).Returns(new ServerConfig());
        configHelper.Setup(c => c.SaveServerConfig(It.IsAny<ServerConfig>()));

        var controller = BuildController(configHelper);
        controller.ControllerContext = BuildContext(isRootUser: true);

        var model = new ServerSettingsViewModel
        {
            Motd = "Test",
            EnableAuth = true,
            LocaleOverride = "en-US",
            LocaleDateTimeOverride = "MM/dd/yyyy",
            MaxDocumentUploadSizeMb = 5,
            EnableReminderEmails = true,
            ReminderEmailDaysAhead = 7
        };

        var result = controller.Index(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.True(controller.TempData.ContainsKey("StatusMessage"));
        Assert.False(string.IsNullOrWhiteSpace(controller.TempData["StatusMessage"] as string));
        configHelper.Verify(c => c.SaveServerConfig(It.Is<ServerConfig>(s =>
            s.Motd == "Test" &&
            s.EnableAuth &&
            s.LocaleOverride == "en-US" &&
            s.LocaleDateTimeOverride == "MM/dd/yyyy" &&
            s.MaxDocumentUploadBytes == 5 * 1024L * 1024L &&
            s.EnableReminderEmails &&
            s.ReminderEmailDaysAhead == 7)), Times.Once);
    }

    [Fact]
    public void Index_Post_InvalidModel_ShowsViewAndDoesNotSave()
    {
        var configHelper = BuildConfigHelperMock();
        var controller = BuildController(configHelper);
        controller.ControllerContext = BuildContext(isRootUser: true);
        controller.ModelState.AddModelError("Motd", "Required");

        var model = new ServerSettingsViewModel { Motd = string.Empty };

        var result = controller.Index(model);

        var view = Assert.IsType<ViewResult>(result);
        var returnedModel = Assert.IsType<ServerSettingsViewModel>(view.Model);
        Assert.Equal(model.Motd, returnedModel.Motd);
        configHelper.Verify(c => c.SaveServerConfig(It.IsAny<ServerConfig>()), Times.Never);
    }

    private Mock<ConfigHelper> BuildConfigHelperMock()
    {
        return new Mock<ConfigHelper>(MockBehavior.Strict, Mock.Of<ILogger<ConfigHelper>>());
    }

    private SettingsController BuildController(Mock<ConfigHelper> configHelper)
    {
        var logger = Mock.Of<ILogger<SettingsController>>();
        var controller = new SettingsController(logger, configHelper.Object);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        return controller;
    }

    private ControllerContext BuildContext(bool isRootUser)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        if (isRootUser)
        {
            claims.Add(new Claim("IsRootUser", "true"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
