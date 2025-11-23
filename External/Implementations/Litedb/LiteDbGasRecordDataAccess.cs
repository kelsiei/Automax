using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.API;
using CarCareTracker.Models.GasRecord;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbGasRecordDataAccess : IGasRecordDataAccess
{
    private readonly ILiteCollection<GasRecord> _collection;

    public LiteDbGasRecordDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<GasRecord>("gas_records");
    }

    public Task<GasRecord?> GetGasRecordAsync(int id)
    {
        var result = _collection.FindById(id);
        return Task.FromResult(result);
    }

    public Task<List<GasRecord>> GetGasRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
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

    public Task<int> SaveGasRecordAsync(GasRecord record)
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

    public Task DeleteGasRecordAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
