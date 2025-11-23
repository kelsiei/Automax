using System.Security.Claims;
using CarCareTracker.Controllers;
using CarCareTracker.Helper;
using CarCareTracker.Logic;
using CarCareTracker.Models.Reminder;
using CarCareTracker.Models.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CarCareTracker.Tests.Controllers;

public class ReminderEmailControllerTests
{
    [Fact]
    public async Task Index_NonRoot_Forbid()
    {
        var controller = BuildController(out _, out _, out _, out _);
        controller.ControllerContext = BuildContext(isRoot: false);

        var result = await controller.Index();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Send_NonRoot_Forbid()
    {
        var controller = BuildController(out _, out _, out _, out _);
        controller.ControllerContext = BuildContext(isRoot: false);

        var result = await controller.Send();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Index_Root_Disabled_ShowsModelAndConfig()
    {
        var controller = BuildController(out var reminderLogic, out var configHelper, out _, out _);
        controller.ControllerContext = BuildContext(isRoot: true);

        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = false, ReminderEmailDaysAhead = null });

        var digests = new List<ReminderEmailDigest>
        {
            new() { UserName = "User", EmailAddress = "u@example.com" }
        };
        reminderLogic.Setup(r => r.BuildReminderEmailDigestsAsync()).ReturnsAsync(digests);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<ReminderEmailDigest>>(view.Model);
        Assert.Equal(digests, model);
        Assert.False((bool)(view.ViewData["EnableReminderEmails"] ?? true));
        Assert.Null(view.ViewData["ReminderEmailDaysAhead"]);
    }

    [Fact]
    public async Task Index_Root_Enabled_ShowsModelAndConfig()
    {
        var controller = BuildController(out var reminderLogic, out var configHelper, out _, out _);
        controller.ControllerContext = BuildContext(isRoot: true);

        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = true, ReminderEmailDaysAhead = 10 });

        var digests = new List<ReminderEmailDigest>
        {
            new() { UserName = "User", EmailAddress = "u@example.com" }
        };
        reminderLogic.Setup(r => r.BuildReminderEmailDigestsAsync()).ReturnsAsync(digests);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        Assert.True((bool)(view.ViewData["EnableReminderEmails"] ?? false));
        Assert.Equal(10, view.ViewData["ReminderEmailDaysAhead"]);
    }

    [Fact]
    public async Task Send_Root_Disabled_ShortCircuits()
    {
        var controller = BuildController(out _, out var configHelper, out _, out _);
        controller.ControllerContext = BuildContext(isRoot: true);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = false });

        var result = await controller.Send();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), redirect.ActionName);
        Assert.Equal("Reminder email digests are currently disabled in settings.", controller.TempData["StatusMessage"]);
    }

    [Fact]
    public async Task Send_Root_Enabled_NoDigests()
    {
        var controller = BuildController(out var reminderLogic, out var configHelper, out _, out var mailHelper);
        controller.ControllerContext = BuildContext(isRoot: true);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = true });
        reminderLogic.Setup(r => r.BuildReminderEmailDigestsAsync()).ReturnsAsync(new List<ReminderEmailDigest>());

        var result = await controller.Send();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), redirect.ActionName);
        Assert.Equal("No reminder email digests need to be sent at this time.", controller.TempData["StatusMessage"]);
        mailHelper.Verify(m => m.SendReminderDigestEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Send_Root_Enabled_SendsDigests()
    {
        var controller = BuildController(out var reminderLogic, out var configHelper, out _, out var mailHelper);
        controller.ControllerContext = BuildContext(isRoot: true);
        controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = true });

        var digests = new List<ReminderEmailDigest>
        {
            new()
            {
                UserName = "User1",
                EmailAddress = "one@example.com",
                Reminders = new List<ReminderCalendarItem>
                {
                    new() { ReminderId = 1, Description = "R1", DueDate = DateTime.UtcNow.Date, IsCompleted = false }
                }
            },
            new()
            {
                UserName = "User2",
                EmailAddress = "two@example.com",
                Reminders = new List<ReminderCalendarItem>
                {
                    new() { ReminderId = 2, Description = "R2", DueDate = DateTime.UtcNow.Date, IsCompleted = false }
                }
            }
        };

        reminderLogic.Setup(r => r.BuildReminderEmailDigestsAsync()).ReturnsAsync(digests);
        mailHelper.Setup(m => m.SendReminderDigestEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        var result = await controller.Send();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(controller.Index), redirect.ActionName);
        Assert.Equal("Reminder email digests sent to 2 user(s).", controller.TempData["StatusMessage"]);
        mailHelper.Verify(m => m.SendReminderDigestEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Exactly(2));
    }

    private ReminderEmailController BuildController(out Mock<ReminderEmailLogic> reminderLogic, out ConfigHelper configHelper, out Mock<ILogger<ReminderEmailController>> logger, out Mock<MailHelper> mailHelper)
    {
        logger = new Mock<ILogger<ReminderEmailController>>();
        reminderLogic = new Mock<ReminderEmailLogic>(MockBehavior.Strict, null!, null!, null!);
        var configLogger = Mock.Of<ILogger<ConfigHelper>>();
        configHelper = new ConfigHelper(configLogger);
        mailHelper = new Mock<MailHelper>(MockBehavior.Strict, Mock.Of<ILogger<MailHelper>>(), Options.Create(new ServerConfig()));

        var controller = new ReminderEmailController(logger.Object, reminderLogic.Object, configHelper, mailHelper.Object);
        return controller;
    }

    private ControllerContext BuildContext(bool isRoot)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        claims.Add(new Claim("IsRootUser", isRoot ? "true" : "false"));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
