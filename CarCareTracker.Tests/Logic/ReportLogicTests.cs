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

public class ReportLogicTests : IDisposable
{
    private readonly string _documentsRoot = Path.Combine(StaticHelper.DataDirectory, "documents");

    public ReportLogicTests()
    {
        if (Directory.Exists(_documentsRoot))
        {
            Directory.Delete(_documentsRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GetVehicleReportSummariesAsync_MapsFieldsCorrectly()
    {
        var (vehicleLogic, vehicle1, vehicle2) = BuildVehicleLogicWithData();

        var reportLogic = new ReportLogic(vehicleLogic);
        var summaries = await reportLogic.GetVehicleReportSummariesAsync(1, isRootUser: true);

        var first = summaries.Single(s => s.VehicleId == vehicle1.Id);
        Assert.Equal(vehicle1.Year, first.Year);
        Assert.Equal(vehicle1.Make, first.Make);
        Assert.Equal(vehicle1.Model, first.Model);
        Assert.Equal(vehicle1.LicensePlate, first.LicensePlate);
        Assert.Equal(1000, first.LastReportedMileage);
        Assert.Equal(new DateTime(2024, 2, 1), first.LastServiceDate);
        Assert.Equal(new DateTime(2024, 3, 1), first.LastGasDate);
        Assert.Equal(150m, first.TotalCost);
        Assert.Equal(0.1500m, first.CostPerMile);
        Assert.Equal(1, first.ActivePlanCount);
        Assert.Equal(2, first.NoteCount);
        Assert.Equal(1, first.DocumentCount);
        Assert.False(first.HasUrgentReminders);

        var second = summaries.Single(s => s.VehicleId == vehicle2.Id);
        Assert.Equal(vehicle2.Year, second.Year);
        Assert.Equal(vehicle2.Make, second.Make);
        Assert.Equal(vehicle2.Model, second.Model);
        Assert.Equal(vehicle2.LicensePlate, second.LicensePlate);
        Assert.Equal(500, second.LastReportedMileage);
        Assert.Null(second.TotalCost);
        Assert.Null(second.CostPerMile);
    }

    [Fact]
    public async Task GetVehicleReportCsvAsync_IncludesAllColumnsAndRows()
    {
        var (vehicleLogic, vehicle1, vehicle2) = BuildVehicleLogicWithData();
        var reportLogic = new ReportLogic(vehicleLogic);

        var csv = await reportLogic.GetVehicleReportCsvAsync(1, isRootUser: true);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Assert.StartsWith("VehicleId,Year,Make,Model,LicensePlate,LastReportedMileage,LastServiceDate,LastGasDate,TotalCost,CostPerMile,ActivePlanCount,NoteCount,DocumentCount,HasUrgentReminders", lines[0]);
        Assert.Equal(3, lines.Length); // header + 2 data lines

        Assert.Contains(lines, l => l.Contains(vehicle1.Id.ToString()) && l.Contains("150.00") && l.Contains("0.1500") && l.Contains("1,2020"));
        Assert.Contains(lines, l => l.Contains(vehicle2.Id.ToString()) && l.Contains("\"Ford, \"\"Co\"\"\"") && l.Contains("\"Model, X\""));
    }

    [Fact]
    public async Task GetVehicleReportCsvAsync_EscapesTextFields()
    {
        var (vehicleLogic, _, vehicle2) = BuildVehicleLogicWithData();
        var reportLogic = new ReportLogic(vehicleLogic);

        var csv = await reportLogic.GetVehicleReportCsvAsync(1, isRootUser: true);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var dataLine = lines.Single(l => l.StartsWith(vehicle2.Id.ToString() + ","));

        // Make and Model contain commas/quotes and should be quoted with internal quotes doubled.
        Assert.Contains("\"Ford, \"\"Co\"\"\"", dataLine);
        Assert.Contains("\"Model, X\"", dataLine);
        Assert.Contains("\"ABC\"\"123\"", dataLine);
    }

    private (VehicleLogic vehicleLogic, Vehicle vehicle1, Vehicle vehicle2) BuildVehicleLogicWithData()
    {
        var vehicle1 = new Vehicle { Id = 1, Year = 2020, Make = "Make1", Model = "Model1", LicensePlate = "PLATE1" };
        var vehicle2 = new Vehicle { Id = 2, Year = 2021, Make = "Ford, \"Co\"", Model = "Model, X", LicensePlate = "ABC\"123" };

        var vehicles = new List<Vehicle> { vehicle1, vehicle2 };

        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>())).ReturnsAsync(vehicles);

        var gasData = new Mock<IGasRecordDataAccess>();
        gasData.Setup(g => g.GetGasRecordsForVehicleAsync(vehicle1.Id, null)).ReturnsAsync(new List<GasRecord>
        {
            new() { VehicleId = vehicle1.Id, Date = new DateTime(2024, 3, 1), TotalCost = 100m }
        });
        gasData.Setup(g => g.GetGasRecordsForVehicleAsync(vehicle2.Id, null)).ReturnsAsync(new List<GasRecord>
        {
            new() { VehicleId = vehicle2.Id, Date = new DateTime(2024, 4, 1), TotalCost = 0m }
        });

        var serviceData = new Mock<IServiceRecordDataAccess>();
        serviceData.Setup(s => s.GetServiceRecordsForVehicleAsync(vehicle1.Id, null)).ReturnsAsync(new List<ServiceRecord>
        {
            new() { VehicleId = vehicle1.Id, Date = new DateTime(2024, 2, 1), Cost = 50m }
        });
        serviceData.Setup(s => s.GetServiceRecordsForVehicleAsync(vehicle2.Id, null)).ReturnsAsync(new List<ServiceRecord>());

        var odometerData = new Mock<IOdometerRecordDataAccess>();
        odometerData.Setup(o => o.GetOdometerRecordsForVehicleAsync(vehicle1.Id, null)).ReturnsAsync(new List<OdometerRecord>
        {
            new() { VehicleId = vehicle1.Id, Date = new DateTime(2024, 1, 1), Odometer = 0 },
            new() { VehicleId = vehicle1.Id, Date = new DateTime(2024, 5, 1), Odometer = 1000 }
        });
        odometerData.Setup(o => o.GetOdometerRecordsForVehicleAsync(vehicle2.Id, null)).ReturnsAsync(new List<OdometerRecord>
        {
            new() { VehicleId = vehicle2.Id, Date = new DateTime(2024, 1, 1), Odometer = 500 }
        });

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(It.IsAny<int>(), null)).ReturnsAsync(new List<ReminderRecord>());

        var planData = new Mock<IPlanRecordDataAccess>();
        planData.Setup(p => p.GetPlanRecordsForVehicleAsync(vehicle1.Id, null)).ReturnsAsync(new List<PlanRecord>
        {
            new() { VehicleId = vehicle1.Id, Progress = PlanProgress.NotStarted, IsArchived = false }
        });
        planData.Setup(p => p.GetPlanRecordsForVehicleAsync(vehicle2.Id, null)).ReturnsAsync(new List<PlanRecord>());

        var noteData = new Mock<INoteDataAccess>();
        noteData.Setup(n => n.GetNotesForVehicleAsync(vehicle1.Id, null)).ReturnsAsync(new List<Note>
        {
            new() { VehicleId = vehicle1.Id },
            new() { VehicleId = vehicle1.Id }
        });
        noteData.Setup(n => n.GetNotesForVehicleAsync(vehicle2.Id, null)).ReturnsAsync(new List<Note>());

        // Document setup
        CreateDocumentForVehicle(vehicle1.Id, "doc1.pdf");
        CreateDocumentForVehicle(vehicle2.Id, "doc2.pdf");

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

        return (vehicleLogic, vehicle1, vehicle2);
    }

    private void CreateDocumentForVehicle(int vehicleId, string fileName)
    {
        var vehicleDir = Path.Combine(_documentsRoot, vehicleId.ToString());
        Directory.CreateDirectory(vehicleDir);
        File.WriteAllText(Path.Combine(vehicleDir, fileName), "content");
    }

    public void Dispose()
    {
        if (Directory.Exists(_documentsRoot))
        {
            Directory.Delete(_documentsRoot, recursive: true);
        }
    }
}
