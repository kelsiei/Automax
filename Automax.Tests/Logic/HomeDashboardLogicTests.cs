using Automax.Enum;
#nullable enable
using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Logic;
using Automax.Models.GasRecord;
using Automax.Models.OdometerRecord;
using Automax.Models.PlanRecord;
using Automax.Models.Reminder;
using Automax.Models.ServiceRecord;
using Automax.Models.Settings;
using Automax.Models.User;
using Automax.Models.Vehicle;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Automax.Tests.Logic;

public class HomeDashboardLogicTests : IDisposable
{
    private readonly string _configDir = StaticHelper.ConfigDirectory;
    private readonly string _documentsRoot = Path.Combine(StaticHelper.DataDirectory, "documents");

    public HomeDashboardLogicTests()
    {
        Cleanup();
    }

    [Fact]
    public async Task BuildDashboardAsync_AnonymousUserReturnsMotdOnly()
    {
        var configLogger = Mock.Of<ILogger<ConfigHelper>>();
        var configHelper = new ConfigHelper(configLogger);
        configHelper.SaveServerConfig(new ServerConfig { Motd = "Hello MOTD" });

        var vehicleLogic = BuildVehicleLogicWithNoData();
        var reminderLogic = BuildReminderLogicWithNoData();
        var userLogic = new UserLogic(Mock.Of<IUserAccessDataAccess>());
        var serviceData = new Mock<IServiceRecordDataAccess>().Object;
        var gasData = new Mock<IGasRecordDataAccess>().Object;

        var dashboardLogic = new HomeDashboardLogic(configHelper, vehicleLogic, reminderLogic, userLogic, serviceData, gasData);

        var result = await dashboardLogic.BuildDashboardAsync(null, isRootUser: false);

        Assert.Equal("Hello MOTD", result.Motd);
        Assert.Equal(0, result.VehicleCount);
        Assert.Equal(0, result.VehiclesWithUrgentReminders);
        Assert.Equal(0, result.OpenRemindersCount);
        Assert.Empty(result.UpcomingReminders);
    }

    [Fact]
    public async Task BuildDashboardAsync_AuthenticatedUserAggregatesMetrics()
    {
        var configLogger = Mock.Of<ILogger<ConfigHelper>>();
        var configHelper = new ConfigHelper(configLogger);
        configHelper.SaveServerConfig(new ServerConfig { Motd = "Hello" });

        var today = DateTime.UtcNow.Date;

        var vehicle1 = new Vehicle { Id = 1, Year = 2020, Make = "A", Model = "A", LicensePlate = "A" };
        var vehicle2 = new Vehicle { Id = 2, Year = 2021, Make = "B", Model = "B", LicensePlate = "B" };

        var vehicles = new List<Vehicle> { vehicle1, vehicle2 };

        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<IEnumerable<int>?>()))
            .ReturnsAsync(vehicles);

        var gasData = new Mock<IGasRecordDataAccess>();
        gasData.Setup(g => g.GetGasRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<GasRecord>());

        var serviceData = new Mock<IServiceRecordDataAccess>();
        serviceData.Setup(s => s.GetServiceRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<ServiceRecord>());

        var odometerData = new Mock<IOdometerRecordDataAccess>();
        odometerData.Setup(o => o.GetOdometerRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<OdometerRecord>
        {
            new() { VehicleId = vehicle1.Id, Date = today, Odometer = 1000 }
        });

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(vehicle1.Id, null)).ReturnsAsync(new List<ReminderRecord>
        {
            new() { VehicleId = vehicle1.Id, Metric = ReminderMetric.Date, DueDate = today.AddDays(10), IsCompleted = false, Urgency = ReminderUrgency.Urgent, Description = "Upcoming 1" },
            new() { VehicleId = vehicle1.Id, Metric = ReminderMetric.Date, DueDate = today.AddDays(40), IsCompleted = true, Urgency = ReminderUrgency.NotUrgent, Description = "Outside window completed" }
        });
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(vehicle2.Id, null)).ReturnsAsync(new List<ReminderRecord>
        {
            new() { VehicleId = vehicle2.Id, Metric = ReminderMetric.Odometer, DueDate = today.AddDays(5), IsCompleted = false, Urgency = ReminderUrgency.NotUrgent, Description = "Odo upcoming", DueOdometer = 20000 }
        });

        var planData = new Mock<IPlanRecordDataAccess>();
        planData.Setup(p => p.GetPlanRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<PlanRecord>());

        var noteData = new Mock<INoteDataAccess>();
        noteData.Setup(n => n.GetNotesForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<Models.Note.Note>());

        var fileHelperLogger = Mock.Of<ILogger<FileHelper>>();
        var fileHelper = new FileHelper(fileHelperLogger);

        var vehicleLogic = new VehicleLogic(
            vehicleData.Object,
            gasData.Object,
            serviceData.Object,
            odometerData.Object,
            reminderData.Object,
            planData.Object,
            noteData.Object,
            fileHelper);

        var userAccess = new Mock<IUserAccessDataAccess>();
        userAccess.Setup(u => u.GetUserAccessForUserAsync(It.IsAny<int>())).ReturnsAsync(new List<UserAccess>());
        var userLogic = new UserLogic(userAccess.Object);

        var reminderLogic = new ReminderLogic(userLogic, vehicleData.Object, reminderData.Object);

        var dashboardLogic = new HomeDashboardLogic(configHelper, vehicleLogic, reminderLogic, userLogic, serviceData.Object, gasData.Object);

        var result = await dashboardLogic.BuildDashboardAsync(1, isRootUser: true);

        Assert.Equal(2, result.VehicleCount);
        Assert.Equal(1, result.VehiclesWithUrgentReminders);
        Assert.Equal(2, result.OpenRemindersCount); // both non-completed reminders with due dates
        Assert.Equal(2, result.UpcomingReminders.Count);
        Assert.Equal(today.AddDays(5), result.UpcomingReminders[0].DueDate);
    }

    private VehicleLogic BuildVehicleLogicWithNoData()
    {
        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<IEnumerable<int>?>()))
            .ReturnsAsync(new List<Vehicle>());

        var gasData = new Mock<IGasRecordDataAccess>();
        gasData.Setup(g => g.GetGasRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<Automax.Models.GasRecord.GasRecord>());

        var serviceData = new Mock<IServiceRecordDataAccess>();
        serviceData.Setup(s => s.GetServiceRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<Automax.Models.ServiceRecord.ServiceRecord>());

        var odometerData = new Mock<IOdometerRecordDataAccess>();
        odometerData.Setup(o => o.GetOdometerRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<Automax.Models.OdometerRecord.OdometerRecord>());

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<ReminderRecord>());

        var planData = new Mock<IPlanRecordDataAccess>();
        planData.Setup(p => p.GetPlanRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<PlanRecord>());

        var noteData = new Mock<INoteDataAccess>();
        noteData.Setup(n => n.GetNotesForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<Models.Note.Note>());

        var fileHelperLogger = Mock.Of<ILogger<FileHelper>>();
        var fileHelper = new FileHelper(fileHelperLogger);

        return new VehicleLogic(
            vehicleData.Object,
            gasData.Object,
            serviceData.Object,
            odometerData.Object,
            reminderData.Object,
            planData.Object,
            noteData.Object,
            fileHelper);
    }

    private ReminderLogic BuildReminderLogicWithNoData()
    {
        var userAccess = new Mock<IUserAccessDataAccess>();
        userAccess.Setup(u => u.GetUserAccessForUserAsync(It.IsAny<int>())).ReturnsAsync(new List<UserAccess>());
        var userLogic = new UserLogic(userAccess.Object);

        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<IEnumerable<int>?>()))
            .ReturnsAsync(new List<Vehicle>());

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<ReminderRecord>());

        return new ReminderLogic(userLogic, vehicleData.Object, reminderData.Object);
    }

    public void Dispose()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (Directory.Exists(_configDir))
        {
            Directory.Delete(_configDir, recursive: true);
        }

        if (Directory.Exists(_documentsRoot))
        {
            Directory.Delete(_documentsRoot, recursive: true);
        }
    }
}
