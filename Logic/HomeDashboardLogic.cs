using CarCareTracker.Helper;
using CarCareTracker.Models.Home;

namespace CarCareTracker.Logic;

public class HomeDashboardLogic
{
    private readonly ConfigHelper _configHelper;
    private readonly VehicleLogic _vehicleLogic;
    private readonly ReminderLogic _reminderLogic;

    public HomeDashboardLogic(
        ConfigHelper configHelper,
        VehicleLogic vehicleLogic,
        ReminderLogic reminderLogic)
    {
        _configHelper = configHelper;
        _vehicleLogic = vehicleLogic;
        _reminderLogic = reminderLogic;
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

        var vehicles = await _vehicleLogic.GetVehicleDashboardAsync(userId.Value, isRootUser, null);
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

        return vm;
    }
}

