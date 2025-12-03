using Automax.Models.API;
using Automax.Models.Reminder;

namespace Automax.External.Interfaces;

public interface IReminderRecordDataAccess
{
    Task<ReminderRecord?> GetReminderRecordAsync(int id);
    Task<List<ReminderRecord>> GetReminderRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SaveReminderRecordAsync(ReminderRecord record);
    Task DeleteReminderRecordAsync(int id);
}
