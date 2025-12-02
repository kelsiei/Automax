using Automax.Helper;
using Automax.Logic;
using Automax.Models.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Automax.External.Implementations.Postgres;
using Automax.External.Interfaces;
using Xunit;

namespace Automax.Tests;

public class HomeDashboardIntegrationTests
{
    [PostgresIntegrationFact]
    public async Task Dashboard_ShowsRemindersServicesFuel()
    {
        var connString = Environment.GetEnvironmentVariable("AUTOMAX_POSTGRES_CONNECTION_STRING")
                         ?? "Host=localhost;Database=automax;Username=automax;Password=pass";

        var options = Options.Create(new ServerConfig
        {
            StorageProvider = "Postgres",
            PostgresConnectionString = connString
        });

        var pgFactory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        var vehicleData = new PostgresVehicleDataAccess(pgFactory, NullLogger<PostgresVehicleDataAccess>.Instance);
        var gasData = new PostgresGasRecordDataAccess(pgFactory, NullLogger<PostgresGasRecordDataAccess>.Instance);
        var serviceData = new PostgresServiceRecordDataAccess(pgFactory, NullLogger<PostgresServiceRecordDataAccess>.Instance);
        var odometerData = new PostgresOdometerDataAccess(pgFactory, NullLogger<PostgresOdometerDataAccess>.Instance);
        var reminderData = new PostgresReminderRecordDataAccess(pgFactory, NullLogger<PostgresReminderRecordDataAccess>.Instance);
        var planData = new PostgresPlanRecordDataAccess(pgFactory, NullLogger<PostgresPlanRecordDataAccess>.Instance);
        var noteData = new PostgresNoteDataAccess(pgFactory, NullLogger<PostgresNoteDataAccess>.Instance);
        var userAccessData = new PostgresUserAccessDataAccess(pgFactory, NullLogger<PostgresUserAccessDataAccess>.Instance);
        var userData = new PostgresUserRecordDataAccess(pgFactory, NullLogger<PostgresUserRecordDataAccess>.Instance);

        var fileHelper = new FileHelper(NullLogger<FileHelper>.Instance);
        var configHelper = new ConfigHelper(NullLogger<ConfigHelper>.Instance);
        var userLogic = new UserLogic(userAccessData);
        var reminderLogic = new ReminderLogic(userLogic, vehicleData, reminderData);
        var vehicleLogic = new VehicleLogic(vehicleData, gasData, serviceData, odometerData, reminderData, planData, noteData, fileHelper);
        var dashboardLogic = new HomeDashboardLogic(configHelper, vehicleLogic, reminderLogic, userLogic, serviceData, gasData);

        // Seed minimal data for the dashboard
        var vehicle = new Automax.Models.Vehicle.Vehicle
        {
            Year = 2020,
            Make = "Dash",
            Model = "Test",
            LicensePlate = $"DASH-{Guid.NewGuid():N}".Substring(0, 8)
        };
        var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

        var serviceRecord = new Automax.Models.ServiceRecord.ServiceRecord
        {
            VehicleId = vehicleId,
            Date = DateTime.UtcNow.Date.AddDays(-3),
            Description = "Oil Change",
            Cost = 75m
        };
        await serviceData.SaveServiceRecordAsync(serviceRecord);

        var gasRecord = new Automax.Models.GasRecord.GasRecord
        {
            VehicleId = vehicleId,
            Date = DateTime.UtcNow.Date.AddDays(-2),
            Volume = 10,
            PricePerUnit = 4,
            TotalCost = 40,
            Odometer = 12345
        };
        await gasData.SaveGasRecordAsync(gasRecord);

        var reminder = new Automax.Models.Reminder.ReminderRecord
        {
            VehicleId = vehicleId,
            Description = "Dashboard reminder",
            Metric = Automax.Enum.ReminderMetric.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(5),
            Urgency = Automax.Enum.ReminderUrgency.Urgent
        };
        await reminderData.SaveReminderRecordAsync(reminder);

        var vm = await dashboardLogic.BuildDashboardAsync(userId: 1, isRootUser: true);

        Assert.True(vm.VehicleCount > 0);
        Assert.True(vm.UpcomingReminders.Count > 0);
        Assert.True(vm.RecentServices.Count > 0);
        Assert.True(vm.FuelSummaries.Count > 0);

        await reminderData.DeleteReminderRecordAsync(reminder.Id);
        await gasData.DeleteGasRecordAsync(gasRecord.Id);
        await serviceData.DeleteServiceRecordAsync(serviceRecord.Id);
        await vehicleData.DeleteVehicleAsync(vehicleId);
    }
}
