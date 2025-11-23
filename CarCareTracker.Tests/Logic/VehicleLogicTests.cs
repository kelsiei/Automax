using CarCareTracker.Enum;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Logic;
using CarCareTracker.Models.GasRecord;
using CarCareTracker.Models.Note;
using CarCareTracker.Models.OdometerRecord;
using CarCareTracker.Models.PlanRecord;
using CarCareTracker.Models.Reminder;
using CarCareTracker.Models.ServiceRecord;
using CarCareTracker.Models.Vehicle;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarCareTracker.Tests.Logic;

public class VehicleLogicTests : IDisposable
{
    private readonly string _documentsRoot = Path.Combine(StaticHelper.DataDirectory, "documents");

    public VehicleLogicTests()
    {
        // Ensure clean documents directory before each run.
        if (Directory.Exists(_documentsRoot))
        {
            Directory.Delete(_documentsRoot, recursive: true);
        }
    }

    [Fact]
    public async Task BuildVehicleViewModel_ComputesCostsMileageAndCounts()
    {
        var vehicle = new Vehicle { Id = 1, Year = 2020, Make = "Test", Model = "Car", LicensePlate = "ABC123" };

        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>())).ReturnsAsync(new List<Vehicle> { vehicle });

        var gasData = new Mock<IGasRecordDataAccess>();
        gasData.Setup(g => g.GetGasRecordsForVehicleAsync(vehicle.Id, null)).ReturnsAsync(new List<GasRecord>
        {
            new() { VehicleId = vehicle.Id, Date = new DateTime(2024, 1, 1), TotalCost = 100m },
            new() { VehicleId = vehicle.Id, Date = new DateTime(2024, 6, 1), TotalCost = 50m }
        });

        var serviceData = new Mock<IServiceRecordDataAccess>();
        serviceData.Setup(s => s.GetServiceRecordsForVehicleAsync(vehicle.Id, null)).ReturnsAsync(new List<ServiceRecord>
        {
            new() { VehicleId = vehicle.Id, Date = new DateTime(2024, 5, 1), Cost = 200m },
            new() { VehicleId = vehicle.Id, Date = new DateTime(2024, 7, 1), Cost = 25m }
        });

        var odometerData = new Mock<IOdometerRecordDataAccess>();
        odometerData.Setup(o => o.GetOdometerRecordsForVehicleAsync(vehicle.Id, null)).ReturnsAsync(new List<OdometerRecord>
        {
            new() { VehicleId = vehicle.Id, Date = new DateTime(2024, 1, 1), Odometer = 5000 },
            new() { VehicleId = vehicle.Id, Date = new DateTime(2024, 8, 1), Odometer = 10000 }
        });

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(vehicle.Id, null)).ReturnsAsync(new List<ReminderRecord>
        {
            new() { VehicleId = vehicle.Id, Urgency = ReminderUrgency.Urgent, IsCompleted = false }
        });

        var planData = new Mock<IPlanRecordDataAccess>();
        planData.Setup(p => p.GetPlanRecordsForVehicleAsync(vehicle.Id, null)).ReturnsAsync(new List<PlanRecord>
        {
            new() { VehicleId = vehicle.Id, Progress = PlanProgress.InProgress, IsArchived = false },
            new() { VehicleId = vehicle.Id, Progress = PlanProgress.NotStarted, IsArchived = false },
            new() { VehicleId = vehicle.Id, Progress = PlanProgress.Done, IsArchived = false }, // not counted
            new() { VehicleId = vehicle.Id, Progress = PlanProgress.InProgress, IsArchived = true } // not counted
        });

        var noteData = new Mock<INoteDataAccess>();
        noteData.Setup(n => n.GetNotesForVehicleAsync(vehicle.Id, null)).ReturnsAsync(new List<Note>
        {
            new() { VehicleId = vehicle.Id },
            new() { VehicleId = vehicle.Id },
            new() { VehicleId = vehicle.Id }
        });

        // Prepare documents
        var vehicleDocDir = Path.Combine(_documentsRoot, vehicle.Id.ToString());
        Directory.CreateDirectory(vehicleDocDir);
        File.WriteAllText(Path.Combine(vehicleDocDir, "file1.pdf"), "content");
        File.WriteAllText(Path.Combine(vehicleDocDir, "file2.pdf"), "content");

        var fileHelperLogger = Mock.Of<ILogger<FileHelper>>();
        var fileHelper = new FileHelper(fileHelperLogger);

        var logic = new VehicleLogic(
            vehicleData.Object,
            gasData.Object,
            serviceData.Object,
            odometerData.Object,
            reminderData.Object,
            planData.Object,
            noteData.Object,
            fileHelper);

        var result = await logic.GetVehicleDashboardAsync(1, isRootUser: true, allowedVehicleIds: null);
        var vm = result.Single();

        Assert.Equal(10000, vm.LastReportedMileage);
        Assert.Equal(375m, vm.TotalCost); // 100+50+200+25
        Assert.Equal(0.075m, vm.CostPerMile); // 375 / (10000-5000)
        Assert.Equal(new DateTime(2024, 7, 1), vm.LastServiceDate);
        Assert.Equal(new DateTime(2024, 6, 1), vm.LastGasDate);
        Assert.True(vm.HasUrgentReminders);
        Assert.Equal(2, vm.ActivePlanCount);
        Assert.Equal(3, vm.NoteCount);
        Assert.Equal(2, vm.DocumentCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_documentsRoot))
        {
            Directory.Delete(_documentsRoot, recursive: true);
        }
    }
}
