using System.Security.Claims;
using Automax.Controllers;
using Automax.External.Interfaces;
using Automax.Logic;
using Automax.Models.Vehicle;
using Automax.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Automax.Tests.Controllers;

public class VehicleControllerTests
{
    [Fact]
    public async Task Index_NonAdmin_WithOneVehicle_ReturnsOnlyOwnVehicle()
    {
        var vehicle = new VehicleViewModel { Vehicle = new Vehicle { Id = 1, Make = "User1" } };
        var vehicleLogic = new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!);
        vehicleLogic.Setup(v => v.GetVehicleDashboardAsync(1, false, It.IsAny<IEnumerable<int>>(), null))
            .ReturnsAsync(new List<VehicleViewModel> { vehicle });

        var userLogic = new Mock<UserLogic>(MockBehavior.Strict, null!);
        userLogic.Setup(u => u.GetAccessibleVehicleIdsForUserAsync(1, false))
            .ReturnsAsync(new List<int> { 1 });

        var controller = BuildController(vehicleLogic.Object, userLogic.Object);
        controller.ControllerContext = BuildContext(userId: "1", isRoot: false);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<VehicleIndexViewModel>(view.Model);
        var vehicles = model.Vehicles.ToList();
        Assert.Single(vehicles);
        Assert.Equal(1, vehicles[0].Vehicle.Id);
    }

    [Fact]
    public async Task Index_NonAdmin_WithNoVehicles_ReturnsEmpty()
    {
        var vehicleLogic = new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!);
        vehicleLogic.Setup(v => v.GetVehicleDashboardAsync(1, false, It.IsAny<IEnumerable<int>>(), null))
            .ReturnsAsync(new List<VehicleViewModel>());

        var userLogic = new Mock<UserLogic>(MockBehavior.Strict, null!);
        userLogic.Setup(u => u.GetAccessibleVehicleIdsForUserAsync(1, false))
            .ReturnsAsync(new List<int>());

        var controller = BuildController(vehicleLogic.Object, userLogic.Object);
        controller.ControllerContext = BuildContext(userId: "1", isRoot: false);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<VehicleIndexViewModel>(view.Model);
        Assert.Empty(model.Vehicles);
    }

    [Fact]
    public async Task Index_Admin_SeesAllVehicles()
    {
        var vehicles = new List<VehicleViewModel>
        {
            new() { Vehicle = new Vehicle { Id = 1, Make = "User1" } },
            new() { Vehicle = new Vehicle { Id = 2, Make = "User2" } }
        };

        var vehicleLogic = new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!);
        vehicleLogic.Setup(v => v.GetVehicleDashboardAsync(1, true, null, null))
            .ReturnsAsync(vehicles);

        var userLogic = new Mock<UserLogic>(MockBehavior.Strict, null!);
        userLogic.Setup(u => u.GetAccessibleVehicleIdsForUserAsync(1, true))
            .ReturnsAsync(new List<int>());

        var controller = BuildController(vehicleLogic.Object, userLogic.Object);
        controller.ControllerContext = BuildContext(userId: "1", isRoot: true);

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<VehicleIndexViewModel>(view.Model);
        Assert.Equal(2, model.Vehicles.Count());
    }

    private VehicleController BuildController(VehicleLogic vehicleLogic, UserLogic userLogic)
    {
        var logger = Mock.Of<ILogger<VehicleController>>();
        var vehicleData = Mock.Of<IVehicleDataAccess>();
        var userAccess = Mock.Of<IUserAccessDataAccess>();
        return new VehicleController(logger, vehicleData, vehicleLogic, userLogic, userAccess);
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
