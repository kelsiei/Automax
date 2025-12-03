using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Models.GasRecord;
using Automax.Models.OdometerRecord;
using Automax.Models.PlanRecord;
using Automax.Models.Note;
using Automax.Models.Reminder;
using Automax.Models.ServiceRecord;
using Automax.Models.Vehicle;

namespace Automax.Logic;

public class VehicleLogic
{
    private readonly IVehicleDataAccess _vehicleDataAccess;
    private readonly IGasRecordDataAccess _gasRecordDataAccess;
    private readonly IServiceRecordDataAccess _serviceRecordDataAccess;
    private readonly IOdometerRecordDataAccess _odometerRecordDataAccess;
    private readonly IReminderRecordDataAccess _reminderRecordDataAccess;
    private readonly IPlanRecordDataAccess _planRecordDataAccess;
    private readonly INoteDataAccess _noteDataAccess;
    private readonly FileHelper _fileHelper;

    public VehicleLogic(
        IVehicleDataAccess vehicleDataAccess,
        IGasRecordDataAccess gasRecordDataAccess,
        IServiceRecordDataAccess serviceRecordDataAccess,
        IOdometerRecordDataAccess odometerRecordDataAccess,
        IReminderRecordDataAccess reminderRecordDataAccess,
        IPlanRecordDataAccess planRecordDataAccess,
        INoteDataAccess noteDataAccess,
        FileHelper fileHelper)
    {
        _vehicleDataAccess = vehicleDataAccess;
        _gasRecordDataAccess = gasRecordDataAccess;
        _serviceRecordDataAccess = serviceRecordDataAccess;
        _odometerRecordDataAccess = odometerRecordDataAccess;
        _reminderRecordDataAccess = reminderRecordDataAccess;
        _planRecordDataAccess = planRecordDataAccess;
        _noteDataAccess = noteDataAccess;
        _fileHelper = fileHelper;
    }

    /// <summary>
    /// Builds a list of VehicleViewModel instances for the given user, respecting allowed vehicle access.
    /// </summary>
    public virtual async Task<List<VehicleViewModel>> GetVehicleDashboardAsync(int userId, bool isRootUser, IEnumerable<int>? allowedVehicleIds = null, string? searchTerm = null)
    {
        var vehicles = await _vehicleDataAccess.GetVehiclesAsync(userId, isRootUser, allowedVehicleIds);

        if (!isRootUser)
        {
            if (allowedVehicleIds == null)
            {
                return new List<VehicleViewModel>();
            }

            var allowedSet = new HashSet<int>(allowedVehicleIds);
            vehicles = vehicles.Where(v => allowedSet.Contains(v.Id)).ToList();
        }

        var result = new List<VehicleViewModel>();

        foreach (var vehicle in vehicles)
        {
            var viewModel = await BuildVehicleViewModelAsync(vehicle);
            result.Add(viewModel);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            result = result
                .Where(vm =>
                    (vm.Vehicle.Year.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(vm.Vehicle.Make) && vm.Vehicle.Make.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(vm.Vehicle.Model) && vm.Vehicle.Model.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(vm.Vehicle.LicensePlate) && vm.Vehicle.LicensePlate.Contains(term, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        return result;
    }

    /// <summary>
    /// Returns a single VehicleViewModel for the specified vehicle, or null if not found.
    /// </summary>
    public async Task<VehicleViewModel?> GetVehicleDashboardEntryAsync(int vehicleId)
    {
        var vehicle = await _vehicleDataAccess.GetVehicleAsync(vehicleId);
        if (vehicle == null)
        {
            return null;
        }

        return await BuildVehicleViewModelAsync(vehicle);
    }

    private async Task<VehicleViewModel> BuildVehicleViewModelAsync(Vehicle vehicle)
    {
        // Load related records. For now, we use simple queries per vehicle; optimization can be added later.
        var odometerRecords = await _odometerRecordDataAccess.GetOdometerRecordsForVehicleAsync(vehicle.Id, null);
        var gasRecords = await _gasRecordDataAccess.GetGasRecordsForVehicleAsync(vehicle.Id, null);
        var serviceRecords = await _serviceRecordDataAccess.GetServiceRecordsForVehicleAsync(vehicle.Id, null);

        int? lastOdometer = null;
        if (odometerRecords.Count > 0)
        {
            var latest = odometerRecords.OrderByDescending(x => x.Date).First();
            lastOdometer = latest.Odometer;
        }

        decimal totalCost = 0m;
        if (gasRecords.Count > 0)
        {
            totalCost += gasRecords.Sum(g => g.TotalCost);
        }

        if (serviceRecords.Count > 0)
        {
            totalCost += serviceRecords.Sum(s => s.Cost ?? 0m);
        }

        DateTime? lastServiceDate = null;
        if (serviceRecords.Count > 0)
        {
            lastServiceDate = serviceRecords.Max(s => s.Date);
        }

        DateTime? lastGasDate = null;
        if (gasRecords.Count > 0)
        {
            lastGasDate = gasRecords.Max(g => g.Date);
        }

        decimal? costPerMile = null;
        if (odometerRecords.Count >= 2)
        {
            var minOdo = odometerRecords.Min(x => x.Odometer);
            var maxOdo = odometerRecords.Max(x => x.Odometer);
            var distance = maxOdo - minOdo;
            if (distance > 0)
            {
                costPerMile = totalCost / distance;
            }
        }

        var hasUrgentReminders = await HasUrgentRemindersAsync(vehicle.Id);

        var planRecords = await _planRecordDataAccess.GetPlanRecordsForVehicleAsync(vehicle.Id, null);
        var activePlanCount = planRecords
            .Count(p => !p.IsArchived && p.Progress != Enum.PlanProgress.Done);

        var notes = await _noteDataAccess.GetNotesForVehicleAsync(vehicle.Id, null);
        var noteCount = notes.Count;

        var documents = await _fileHelper.GetVehicleDocumentsAsync(vehicle.Id);
        var documentCount = documents?.Count ?? 0;

        return new VehicleViewModel
        {
            Vehicle = vehicle,
            LastReportedMileage = lastOdometer,
            TotalCost = totalCost == 0m ? null : totalCost,
            CostPerMile = costPerMile,
            HasUrgentReminders = hasUrgentReminders,
            LastServiceDate = lastServiceDate,
            LastGasDate = lastGasDate,
            ActivePlanCount = activePlanCount,
            NoteCount = noteCount,
            DocumentCount = documentCount
        };
    }

    private async Task<bool> HasUrgentRemindersAsync(int vehicleId)
    {
        var reminders = await _reminderRecordDataAccess.GetReminderRecordsForVehicleAsync(vehicleId, null);
        if (reminders == null || reminders.Count == 0)
        {
            return false;
        }

        foreach (var reminder in reminders)
        {
            if (!reminder.IsCompleted &&
                (reminder.Urgency == Enum.ReminderUrgency.Urgent ||
                 reminder.Urgency == Enum.ReminderUrgency.VeryUrgent ||
                 reminder.Urgency == Enum.ReminderUrgency.PastDue))
            {
                return true;
            }
        }

        return false;
    }
}
