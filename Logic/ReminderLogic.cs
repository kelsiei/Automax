using Automax.Enum;
using Automax.External.Interfaces;
using Automax.Models.Reminder;

namespace Automax.Logic;

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

    public virtual async Task<IList<ReminderCalendarItem>> GetDateBasedRemindersForUserAsync(int userId, bool isRootUser)
    {
        var accessibleVehicleIds = await _userLogic.GetAccessibleVehicleIdsForUserAsync(userId, isRootUser);

        var vehicles = await _vehicleDataAccess.GetVehiclesAsync(userId, isRootUser, accessibleVehicleIds);

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
