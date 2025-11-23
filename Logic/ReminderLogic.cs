using CarCareTracker.Enum;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Models.Reminder;

namespace CarCareTracker.Logic;

public class ReminderLogic
{
    private readonly UserLogic _userLogic;
    private readonly IVehicleDataAccess _vehicleDataAccess;
    private readonly IReminderRecordDataAccess _reminderRecordDataAccess;

    public ReminderLogic(
        UserLogic userLogic,
        IVehicleDataAccess vehicleDataAccess,
        IReminderRecordDataAccess reminderRecordDataAccess)
    {
        _userLogic = userLogic;
        _vehicleDataAccess = vehicleDataAccess;
        _reminderRecordDataAccess = reminderRecordDataAccess;
    }

    public async Task<IList<ReminderCalendarItem>> GetDateBasedRemindersForUserAsync(int userId, bool isRootUser)
    {
        var accessibleVehicleIds = await _userLogic.GetAccessibleVehicleIdsForUserAsync(userId, isRootUser);

        List<Models.Vehicle.Vehicle> vehicles;

        if (isRootUser && accessibleVehicleIds.Count == 0)
        {
            vehicles = await _vehicleDataAccess.GetVehiclesAsync(userId);
        }
        else
        {
            var allVehicles = await _vehicleDataAccess.GetVehiclesAsync(userId);
            var allowedSet = new HashSet<int>(accessibleVehicleIds);
            vehicles = allVehicles.Where(v => allowedSet.Contains(v.Id)).ToList();
        }

        var results = new List<ReminderCalendarItem>();

        foreach (var vehicle in vehicles)
        {
            var reminders = await _reminderRecordDataAccess.GetReminderRecordsForVehicleAsync(vehicle.Id, null);
            foreach (var reminder in reminders)
            {
                if (!reminder.DueDate.HasValue)
                {
                    continue;
                }

                results.Add(new ReminderCalendarItem
                {
                    ReminderId = reminder.Id,
                    VehicleId = vehicle.Id,
                    Year = vehicle.Year,
                    Make = vehicle.Make ?? string.Empty,
                    Model = vehicle.Model ?? string.Empty,
                    LicensePlate = vehicle.LicensePlate ?? string.Empty,
                    Description = reminder.Description ?? string.Empty,
                    DueDate = reminder.DueDate,
                    IsCompleted = reminder.IsCompleted,
                    Urgency = reminder.Urgency,
                    Tags = reminder.Tags ?? string.Empty,
                    TargetOdometer = reminder.DueOdometer
                });
            }
        }

        return results;
    }
}
