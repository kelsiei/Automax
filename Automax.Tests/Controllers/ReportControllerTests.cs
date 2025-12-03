using System.Security.Claims;
using Automax.Controllers;
using Automax.Logic;
using Automax.Models.Report;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Automax.Tests.Controllers;

public class ReportControllerTests
{
    [Fact]
    public async Task Index_NonAdmin_OnlyOwnSummaries()
    {
        var summaries = new List<VehicleReportSummary>
        {
            new() { VehicleId = 1, HasUrgentReminders = false },
        };

        var reportLogic = new Mock<ReportLogic>(MockBehavior.Strict, null!);
        reportLogic.Setup(r => r.GetVehicleReportSummariesAsync(1, false, null, It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(summaries);

        var userLogic = new Mock<Automax.Logic.UserLogic>(MockBehavior.Strict, null!);
        userLogic.Setup(u => u.GetAccessibleVehicleIdsForUserAsync(1, false))
            .ReturnsAsync(new List<int> { 1 });

        var controller = BuildController(reportLogic.Object, userLogic.Object);
        controller.ControllerContext = BuildContext(userId: "1", isRoot: false);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ReportIndexViewModel>(view.Model);
        Assert.Single(model.VehicleSummaries);
        Assert.Equal(1, model.VehicleSummaries.First().VehicleId);
    }

    [Fact]
    public async Task Index_Admin_SeesAllSummaries()
    {
        var summaries = new List<VehicleReportSummary>
        {
            new() { VehicleId = 1 },
            new() { VehicleId = 2 }
        };

        var reportLogic = new Mock<ReportLogic>(MockBehavior.Strict, null!);
        reportLogic.Setup(r => r.GetVehicleReportSummariesAsync(1, true, null, null))
            .ReturnsAsync(summaries);

        var userLogic = new Mock<Automax.Logic.UserLogic>(MockBehavior.Strict, null!);
        userLogic.Setup(u => u.GetAccessibleVehicleIdsForUserAsync(1, true))
            .ReturnsAsync(new List<int>());

        var controller = BuildController(reportLogic.Object, userLogic.Object);
        controller.ControllerContext = BuildContext(userId: "1", isRoot: true);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ReportIndexViewModel>(view.Model);
        Assert.Equal(2, model.VehicleSummaries.Count());
    }

    private ReportController BuildController(ReportLogic reportLogic, Automax.Logic.UserLogic userLogic)
    {
        var logger = Mock.Of<ILogger<ReportController>>();
        return new ReportController(logger, reportLogic, userLogic);
    }

    private ControllerContext BuildContext(string userId, bool isRoot)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("IsRootUser", isRoot ? "true" : "false")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
