using System.Globalization;
using System.Text;
using CarCareTracker.Models.Report;
using CarCareTracker.Models.Vehicle;

namespace CarCareTracker.Logic;

public class ReportLogic
{
    private readonly VehicleLogic _vehicleLogic;

    public ReportLogic(VehicleLogic vehicleLogic)
    {
        _vehicleLogic = vehicleLogic;
    }

    public async Task<IList<VehicleReportSummary>> GetVehicleReportSummariesAsync(int userId, bool isRootUser, string? searchTerm = null)
    {
        var dashboardVehicles = await _vehicleLogic.GetVehicleDashboardAsync(userId, isRootUser, null, searchTerm);

        var summaries = dashboardVehicles
            .Select(MapToSummary)
            .ToList();

        return summaries;
    }

    public async Task<string> GetVehicleReportCsvAsync(int userId, bool isRootUser)
    {
        var summaries = await GetVehicleReportSummariesAsync(userId, isRootUser);

        var sb = new StringBuilder();

        sb.AppendLine("VehicleId,Year,Make,Model,LicensePlate,LastReportedMileage,LastServiceDate,LastGasDate,TotalCost,CostPerMile,ActivePlanCount,NoteCount,DocumentCount,HasUrgentReminders");

        foreach (var v in summaries)
        {
            var vehicleId = v.VehicleId.ToString(CultureInfo.InvariantCulture);
            var year = v.Year.ToString(CultureInfo.InvariantCulture);
            var make = EscapeForCsv(v.Make);
            var model = EscapeForCsv(v.Model);
            var licensePlate = EscapeForCsv(v.LicensePlate);

            var lastMileage = v.LastReportedMileage.HasValue
                ? v.LastReportedMileage.Value.ToString(CultureInfo.InvariantCulture)
                : string.Empty;

            var lastServiceDate = v.LastServiceDate.HasValue
                ? v.LastServiceDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;

            var lastGasDate = v.LastGasDate.HasValue
                ? v.LastGasDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;

            var totalCost = v.TotalCost.HasValue
                ? v.TotalCost.Value.ToString("0.00", CultureInfo.InvariantCulture)
                : string.Empty;

            var costPerMile = v.CostPerMile.HasValue
                ? v.CostPerMile.Value.ToString("0.0000", CultureInfo.InvariantCulture)
                : string.Empty;

            var activePlans = v.ActivePlanCount.ToString(CultureInfo.InvariantCulture);
            var noteCount = v.NoteCount.ToString(CultureInfo.InvariantCulture);
            var documentCount = v.DocumentCount.ToString(CultureInfo.InvariantCulture);
            var hasUrgent = v.HasUrgentReminders ? "true" : "false";

            sb.AppendLine(string.Join(",", new[]
            {
                vehicleId,
                year,
                make,
                model,
                licensePlate,
                lastMileage,
                lastServiceDate,
                lastGasDate,
                totalCost,
                costPerMile,
                activePlans,
                noteCount,
                documentCount,
                hasUrgent
            }));
        }

        return sb.ToString();
    }

    private static VehicleReportSummary MapToSummary(VehicleViewModel vm)
    {
        return new VehicleReportSummary
        {
            VehicleId = vm.Vehicle.Id,
            Year = vm.Vehicle.Year,
            Make = vm.Vehicle.Make ?? string.Empty,
            Model = vm.Vehicle.Model ?? string.Empty,
            LicensePlate = vm.Vehicle.LicensePlate ?? string.Empty,
            LastReportedMileage = vm.LastReportedMileage.HasValue
                ? (int?)Convert.ToInt32(vm.LastReportedMileage.Value)
                : null,
            LastServiceDate = vm.LastServiceDate,
            LastGasDate = vm.LastGasDate,
            TotalCost = vm.TotalCost,
            CostPerMile = vm.CostPerMile,
            ActivePlanCount = vm.ActivePlanCount,
            NoteCount = vm.NoteCount,
            DocumentCount = vm.DocumentCount,
            HasUrgentReminders = vm.HasUrgentReminders
        };
    }

    private static string EscapeForCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");

        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}
