using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.API;
using CarCareTracker.Models.OdometerRecord;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbOdometerRecordDataAccess : IOdometerRecordDataAccess
{
    private readonly ILiteCollection<OdometerRecord> _collection;

    public LiteDbOdometerRecordDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<OdometerRecord>("odometer_records");
    }

    public Task<OdometerRecord?> GetOdometerRecordAsync(int id)
    {
        var result = _collection.FindById(id);
        return Task.FromResult(result);
    }

    public Task<List<OdometerRecord>> GetOdometerRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        var query = _collection.Query()
            .Where(x => x.VehicleId == vehicleId);

        if (filter?.StartDate is not null)
        {
            var start = filter.StartDate.Value;
            query = query.Where(x => x.Date >= start);
        }

        if (filter?.EndDate is not null)
        {
            var end = filter.EndDate.Value;
            query = query.Where(x => x.Date <= end);
        }

        var list = query.ToList();
        return Task.FromResult(list);
    }

    public Task<int> SaveOdometerRecordAsync(OdometerRecord record)
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

    public Task DeleteOdometerRecordAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
