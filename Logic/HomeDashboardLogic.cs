using Automax.Helper;
using Automax.External.Interfaces;
using Automax.Models.Home;

namespace Automax.Logic;

public class HomeDashboardLogic
{
    private readonly ConfigHelper _configHelper;
    private readonly VehicleLogic _vehicleLogic;
    private readonly ReminderLogic _reminderLogic;
    private readonly UserLogic _userLogic;
    private readonly IServiceRecordDataAccess _serviceRecordDataAccess;
    private readonly IGasRecordDataAccess _gasRecordDataAccess;

    public HomeDashboardLogic(
        ConfigHelper configHelper,
        VehicleLogic vehicleLogic,
        ReminderLogic reminderLogic,
        UserLogic userLogic,
        IServiceRecordDataAccess serviceRecordDataAccess,
        IGasRecordDataAccess gasRecordDataAccess)
    {
        _configHelper = configHelper;
        _vehicleLogic = vehicleLogic;
        _reminderLogic = reminderLogic;
        _userLogic = userLogic;
        _serviceRecordDataAccess = serviceRecordDataAccess;
        _gasRecordDataAccess = gasRecordDataAccess;
    }

    public async Task<HomeDashboardViewModel> BuildDashboardAsync(int? userId, bool isRootUser)
    {
        var serverConfig = _configHelper.LoadServerConfig();

        var vm = new HomeDashboardViewModel
        {
            Motd = serverConfig.Motd ?? string.Empty
        };

        if (userId == null)
        {
            return vm;
        }

        var allowedVehicleIds = await _userLogic.GetAccessibleVehicleIdsForUserAsync(userId.Value, isRootUser);
        var vehicles = await _vehicleLogic.GetVehicleDashboardAsync(userId.Value, isRootUser, allowedVehicleIds);
        vm.VehicleCount = vehicles.Count;
        vm.VehiclesWithUrgentReminders = vehicles.Count(v => v.HasUrgentReminders);

        var allDateBasedReminders = await _reminderLogic.GetDateBasedRemindersForUserAsync(userId.Value, isRootUser);

        vm.OpenRemindersCount = allDateBasedReminders.Count(r => !r.IsCompleted);

        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(30);

        vm.UpcomingReminders = allDateBasedReminders
            .Where(r =>
                !r.IsCompleted &&
                r.DueDate.HasValue &&
                r.DueDate.Value.Date >= today &&
                r.DueDate.Value.Date <= cutoff)
            .OrderBy(r => r.DueDate!.Value.Date)
            .ThenBy(r => r.Year)
            .ThenBy(r => r.Make)
            .ThenBy(r => r.Model)
            .ThenBy(r => r.LicensePlate)
            .ThenBy(r => r.Description)
            .Take(20)
            .ToList();

        vm.RecentServices = await BuildRecentServicesAsync(vehicles.Select(v => v.Vehicle.Id).ToList());
        vm.FuelSummaries = await BuildFuelSummariesAsync(vehicles.Select(v => v.Vehicle.Id).ToList());

        return vm;
    }

    private async Task<IList<ServiceSummaryItem>> BuildRecentServicesAsync(IList<int> vehicleIds)
    {
        var recent = new List<ServiceSummaryItem>();

        foreach (var vid in vehicleIds)
        {
            var services = await _serviceRecordDataAccess.GetServiceRecordsForVehicleAsync(vid, null);
            recent.AddRange(services.Select(s => new ServiceSummaryItem
            {
                VehicleId = vid,
                Date = s.Date,
                Description = s.Description ?? string.Empty,
                Cost = s.Cost
            }));
        }

        return recent
            .OrderByDescending(s => s.Date)
            .ThenBy(s => s.VehicleId)
            .Take(10)
            .ToList();
    }

    private async Task<IList<FuelSummaryItem>> BuildFuelSummariesAsync(IList<int> vehicleIds)
    {
        var summaries = new List<FuelSummaryItem>();

        foreach (var vid in vehicleIds)
        {
            var gas = await _gasRecordDataAccess.GetGasRecordsForVehicleAsync(vid, null);
            if (gas.Count == 0)
            {
                continue;
            }

            var avgEfficiency = gas
                .Where(g => g.Volume > 0 && g.Odometer.HasValue)
                .Select(g => g.Odometer!.Value / g.Volume)
                .DefaultIfEmpty(0m)
                .Average();

            var totalCost = gas.Sum(g => g.TotalCost);

            summaries.Add(new FuelSummaryItem
            {
                VehicleId = vid,
                AverageEfficiency = avgEfficiency,
                TotalFuelCost = totalCost
            });
        }

        return summaries;
    }
}
