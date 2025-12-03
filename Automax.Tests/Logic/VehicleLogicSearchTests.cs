using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Logic;
using Automax.Models.Vehicle;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Automax.Tests.Logic;

public class VehicleLogicSearchTests
{
    [Fact]
    public async Task GetVehicleDashboardAsync_FiltersBySearchTerm()
    {
        var vehicles = new List<Vehicle>
        {
            new() { Id = 1, Year = 2015, Make = "Honda", Model = "Civic", LicensePlate = "1B2 3C4" },
            new() { Id = 2, Year = 2020, Make = "Toyota", Model = "Camry", LicensePlate = "XYZ123" }
        };

        var vehicleData = new Mock<IVehicleDataAccess>();
        vehicleData.Setup(v => v.GetVehiclesAsync(It.IsAny<int>(), true, null))
            .ReturnsAsync(vehicles);

        var gasData = new Mock<IGasRecordDataAccess>();
        gasData.Setup(g => g.GetGasRecordsForVehicleAsync(It.IsAny<int>(), null))
            .ReturnsAsync(new List<Models.GasRecord.GasRecord>());

        var serviceData = new Mock<IServiceRecordDataAccess>();
        serviceData.Setup(s => s.GetServiceRecordsForVehicleAsync(It.IsAny<int>(), null))
            .ReturnsAsync(new List<Models.ServiceRecord.ServiceRecord>());

        var odometerData = new Mock<IOdometerRecordDataAccess>();
        odometerData.Setup(o => o.GetOdometerRecordsForVehicleAsync(It.IsAny<int>(), null))
            .ReturnsAsync(new List<Models.OdometerRecord.OdometerRecord>());

        var reminderData = new Mock<IReminderRecordDataAccess>();
        reminderData.Setup(r => r.GetReminderRecordsForVehicleAsync(It.IsAny<int>(), null))
            .ReturnsAsync(new List<Models.Reminder.ReminderRecord>());

        var planData = new Mock<IPlanRecordDataAccess>();
        planData.Setup(p => p.GetPlanRecordsForVehicleAsync(It.IsAny<int>(), null))
            .ReturnsAsync(new List<Models.PlanRecord.PlanRecord>());

        var noteData = new Mock<INoteDataAccess>();
        noteData.Setup(n => n.GetNotesForVehicleAsync(It.IsAny<int>(), null))
            .ReturnsAsync(new List<Models.Note.Note>());

        var fileHelper = new FileHelper(Mock.Of<ILogger<FileHelper>>());

        var logic = new VehicleLogic(
            vehicleData.Object,
            gasData.Object,
            serviceData.Object,
            odometerData.Object,
            reminderData.Object,
            planData.Object,
            noteData.Object,
            fileHelper);

        var result = await logic.GetVehicleDashboardAsync(userId: 0, isRootUser: true, allowedVehicleIds: null, searchTerm: "civ");

        Assert.Single(result);
        var match = result.First();
        Assert.Equal(2015, match.Vehicle.Year);
        Assert.Equal("Honda", match.Vehicle.Make);
        Assert.Equal("Civic", match.Vehicle.Model);
        Assert.Equal("1B2 3C4", match.Vehicle.LicensePlate);
    }
}
