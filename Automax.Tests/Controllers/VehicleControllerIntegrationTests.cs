using System.Security.Claims;
using Automax.Controllers;
using Automax.External.Implementations.Postgres;
using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Logic;
using Automax.Models.Settings;
using Automax.Models.Vehicle;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Automax.Tests.Controllers;

public class VehicleControllerIntegrationTests
{
    [PostgresIntegrationFact]
    public async Task Create_Then_Index_ShowsVehicleDetails()
    {
        var connString = Environment.GetEnvironmentVariable("AUTOMAX_POSTGRES_CONNECTION_STRING")
                         ?? "Host=localhost;Database=automax;Username=automax;Password=pass";

        var loggerFactory = NullLoggerFactory.Instance;
        var serverConfig = Options.Create(new ServerConfig
        {
            StorageProvider = "Postgres",
            PostgresConnectionString = connString
        });

        var pgFactory = new PostgresConnectionFactory(serverConfig, NullLogger<PostgresConnectionFactory>.Instance);
        var vehicleData = new PostgresVehicleDataAccess(pgFactory, loggerFactory.CreateLogger<PostgresVehicleDataAccess>());
        var gasData = new PostgresGasRecordDataAccess(pgFactory, loggerFactory.CreateLogger<PostgresGasRecordDataAccess>());
        var serviceData = new PostgresServiceRecordDataAccess(pgFactory, loggerFactory.CreateLogger<PostgresServiceRecordDataAccess>());
        var odometerData = new PostgresOdometerDataAccess(pgFactory, loggerFactory.CreateLogger<PostgresOdometerDataAccess>());
        var reminderData = new PostgresReminderRecordDataAccess(pgFactory, loggerFactory.CreateLogger<PostgresReminderRecordDataAccess>());
        var planData = new PostgresPlanRecordDataAccess(pgFactory, loggerFactory.CreateLogger<PostgresPlanRecordDataAccess>());
        var noteData = new PostgresNoteDataAccess(pgFactory, loggerFactory.CreateLogger<PostgresNoteDataAccess>());
        var userAccessData = new PostgresUserAccessDataAccess(pgFactory, loggerFactory.CreateLogger<PostgresUserAccessDataAccess>());
        var fileHelper = new FileHelper(loggerFactory.CreateLogger<FileHelper>());

        var vehicleLogic = new VehicleLogic(vehicleData, gasData, serviceData, odometerData, reminderData, planData, noteData, fileHelper);
        var userLogic = new UserLogic(userAccessData);

        var controller = new VehicleController(
            loggerFactory.CreateLogger<VehicleController>(),
            vehicleData,
            vehicleLogic,
            userLogic,
            userAccessData);

        controller.ControllerContext = BuildRootUserContext();

        var vehicle = new Vehicle
        {
            Year = 2015,
            Make = "Honda",
            Model = "Civic",
            LicensePlate = "1B2 3C4"
        };

        var createResult = await controller.Create(vehicle);
        Assert.IsType<RedirectToActionResult>(createResult);

        try
        {
            var indexResult = await controller.Index();
            var view = Assert.IsType<ViewResult>(indexResult);
            var model = Assert.IsType<VehicleIndexViewModel>(view.Model);
            var match = model.Vehicles.FirstOrDefault(v => v.Vehicle.LicensePlate == "1B2 3C4");
            Assert.NotNull(match);
            Assert.Equal(2015, match!.Vehicle.Year);
            Assert.Equal("Honda", match.Vehicle.Make);
            Assert.Equal("Civic", match.Vehicle.Model);
        }
        finally
        {
            // Cleanup created vehicle
            if (vehicle.Id != 0)
            {
                await vehicleData.DeleteVehicleAsync(vehicle.Id);
            }
        }
    }

    private static ControllerContext BuildRootUserContext()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new("IsRootUser", "true")
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
