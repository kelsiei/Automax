using CarCareTracker.Enum;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Logic;
using CarCareTracker.Models.Reminder;
using CarCareTracker.Models.User;
using CarCareTracker.Models.Vehicle;
using Moq;
using Xunit;

namespace CarCareTracker.Tests.Logic;

public class ReminderLogicTests
{
    [Fact]
    public async Task GetDateBasedRemindersForUserAsync_IncludesOnlyAccessibleVehicles()
    {
        var userAccess = new Mock<IUserAccessDataAccess>();
        userAccess.Setup(u => u.GetUserAccessForUserAsync(1)).ReturnsAsync(new List<UserAccess>
        {
            new() { UserId = 1, VehicleId = 1 },
            new() { UserId = 1, VehicleId = 2 }
        });

        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>())).ReturnsAsync(new List<Vehicle>
        {
            new() { Id = 1, Year = 2020, Make = "A", Model = "A", LicensePlate = "A" },
            new() { Id = 2, Year = 2021, Make = "B", Model = "B", LicensePlate = "B" },
            new() { Id = 3, Year = 2022, Make = "C", Model = "C", LicensePlate = "C" }
        });

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(1, null)).ReturnsAsync(new List<ReminderRecord>
        {
            new() { Id = 10, VehicleId = 1, Metric = ReminderMetric.Date, DueDate = new DateTime(2025, 1, 1), Description = "V1" }
        });
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(2, null)).ReturnsAsync(new List<ReminderRecord>
        {
            new() { Id = 20, VehicleId = 2, Metric = ReminderMetric.Odometer, DueDate = new DateTime(2025, 1, 2), Description = "V2", DueOdometer = 12345 }
        });
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(3, null)).ReturnsAsync(new List<ReminderRecord>
        {
            new() { Id = 30, VehicleId = 3, Metric = ReminderMetric.Date, DueDate = new DateTime(2025, 1, 3), Description = "V3" }
        });

        var userLogic = new UserLogic(userAccess.Object);
        var reminderLogic = new ReminderLogic(userLogic, vehicleData.Object, reminderData.Object);

        var results = await reminderLogic.GetDateBasedRemindersForUserAsync(1, isRootUser: false);

        Assert.All(results, r => Assert.Contains(r.VehicleId, new[] { 1, 2 }));
        Assert.DoesNotContain(results, r => r.VehicleId == 3);
    }

    [Fact]
    public async Task GetDateBasedRemindersForUserAsync_IncludesAnyMetricWithDueDate()
    {
        var userAccess = new Mock<IUserAccessDataAccess>();
        userAccess.Setup(u => u.GetUserAccessForUserAsync(1)).ReturnsAsync(new List<UserAccess>());

        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>())).ReturnsAsync(new List<Vehicle>
        {
            new() { Id = 1, Year = 2020, Make = "A", Model = "A", LicensePlate = "A" }
        });

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(1, null)).ReturnsAsync(new List<ReminderRecord>
        {
            new() { Id = 1, VehicleId = 1, Metric = ReminderMetric.Date, DueDate = new DateTime(2025, 1, 1), Description = "Keep" },
            new() { Id = 2, VehicleId = 1, Metric = ReminderMetric.Date, DueDate = null, Description = "No date" },
            new() { Id = 3, VehicleId = 1, Metric = ReminderMetric.Odometer, DueDate = new DateTime(2025, 1, 2), Description = "Odometer", DueOdometer = 50000 }
        });

        var userLogic = new UserLogic(userAccess.Object);
        var reminderLogic = new ReminderLogic(userLogic, vehicleData.Object, reminderData.Object);

        var results = await reminderLogic.GetDateBasedRemindersForUserAsync(1, isRootUser: true);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Description == "Keep");
        Assert.Contains(results, r => r.Description == "Odometer");
        Assert.DoesNotContain(results, r => r.Description == "No date");
    }

    [Fact]
    public async Task GetDateBasedRemindersForUserAsync_MapsFieldsCorrectly()
    {
        var userAccess = new Mock<IUserAccessDataAccess>();
        userAccess.Setup(u => u.GetUserAccessForUserAsync(1)).ReturnsAsync(new List<UserAccess>());

        var vehicle = new Vehicle { Id = 1, Year = 2020, Make = "Make", Model = "Model", LicensePlate = "ABC123" };

        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>())).ReturnsAsync(new List<Vehicle> { vehicle });

        var reminder = new ReminderRecord
        {
            Id = 5,
            VehicleId = vehicle.Id,
            Metric = ReminderMetric.Date,
            DueDate = new DateTime(2025, 2, 1),
            Description = "Desc",
            IsCompleted = false,
            Urgency = ReminderUrgency.VeryUrgent,
            Tags = "tag1",
            DueOdometer = 123456
        };

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(vehicle.Id, null)).ReturnsAsync(new List<ReminderRecord> { reminder });

        var userLogic = new UserLogic(userAccess.Object);
        var reminderLogic = new ReminderLogic(userLogic, vehicleData.Object, reminderData.Object);

        var results = await reminderLogic.GetDateBasedRemindersForUserAsync(1, isRootUser: true);
        var item = results.Single();

        Assert.Equal(vehicle.Id, item.VehicleId);
        Assert.Equal(vehicle.Year, item.Year);
        Assert.Equal(vehicle.Make, item.Make);
        Assert.Equal(vehicle.Model, item.Model);
        Assert.Equal(vehicle.LicensePlate, item.LicensePlate);
        Assert.Equal(reminder.Description, item.Description);
        Assert.Equal(reminder.DueDate, item.DueDate);
        Assert.Equal(reminder.IsCompleted, item.IsCompleted);
        Assert.Equal(reminder.Urgency, item.Urgency);
        Assert.Equal(reminder.Tags, item.Tags);
        Assert.Equal(reminder.DueOdometer, item.TargetOdometer);
    }
}
