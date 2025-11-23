using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.API;
using CarCareTracker.Models.PlanRecord;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbPlanRecordDataAccess : IPlanRecordDataAccess
{
    private readonly ILiteCollection<PlanRecord> _collection;

    public LiteDbPlanRecordDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<PlanRecord>("plan_records");
    }

    public Task<PlanRecord?> GetPlanRecordAsync(int id)
    {
        var result = _collection.FindById(id);
        return Task.FromResult(result);
    }

    public Task<List<PlanRecord>> GetPlanRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        var query = _collection.Query()
            .Where(x => x.VehicleId == vehicleId);

        if (filter?.StartDate is not null)
        {
            var start = filter.StartDate.Value;
            query = query.Where(x => x.PlannedDate == null || x.PlannedDate >= start);
        }

        if (filter?.EndDate is not null)
        {
            var end = filter.EndDate.Value;
            query = query.Where(x => x.PlannedDate == null || x.PlannedDate <= end);
        }

        var list = query.ToList();
        return Task.FromResult(list);
    }

    public Task<int> SavePlanRecordAsync(PlanRecord record)
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

    public Task DeletePlanRecordAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
