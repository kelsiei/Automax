using System.Security.Claims;
using CarCareTracker.Controllers;
using CarCareTracker.Helper;
using CarCareTracker.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarCareTracker.Tests.Controllers;

public class SettingsControllerTests
{
    [Fact]
    public void Index_NonRootUser_Forbid()
    {
        var controller = BuildController();
        controller.ControllerContext = BuildContext(isRootUser: false);

        var result = controller.Index();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void Index_RootUser_ReturnsView()
    {
        var controller = BuildController();
        controller.ControllerContext = BuildContext(isRootUser: true);

        var result = controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ServerSettingsViewModel>(view.Model);
    }

    private SettingsController BuildController()
    {
        var logger = Mock.Of<ILogger<SettingsController>>();
        var configLogger = Mock.Of<ILogger<ConfigHelper>>();
        var configHelper = new ConfigHelper(configLogger);
        return new SettingsController(logger, configHelper);
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
