using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.API;
using CarCareTracker.Models.Reminder;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbReminderRecordDataAccess : IReminderRecordDataAccess
{
    private readonly ILiteCollection<ReminderRecord> _collection;

    public LiteDbReminderRecordDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<ReminderRecord>("reminder_records");
    }

    public Task<ReminderRecord?> GetReminderRecordAsync(int id)
    {
        var result = _collection.FindById(id);
        return Task.FromResult(result);
    }

    public Task<List<ReminderRecord>> GetReminderRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        var query = _collection.Query()
            .Where(x => x.VehicleId == vehicleId);

        if (filter?.StartDate is not null)
        {
            var start = filter.StartDate.Value;
            query = query.Where(x => x.DueDate == null || x.DueDate >= start);
        }

        if (filter?.EndDate is not null)
        {
            var end = filter.EndDate.Value;
            query = query.Where(x => x.DueDate == null || x.DueDate <= end);
        }

        var list = query.ToList();
        return Task.FromResult(list);
    }

    public Task<int> SaveReminderRecordAsync(ReminderRecord record)
    {
        if (record.Id == 0)
        {
            var newId = _collection.Insert(record).AsInt32;
            record.Id = newId;
            return Task.FromResult(newId);
        }

        _collection.Update(record);
        return Task.FromResult(record.Id);
    }

    public Task DeleteReminderRecordAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
