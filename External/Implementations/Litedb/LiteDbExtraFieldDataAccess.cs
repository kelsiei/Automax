using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.Shared;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbExtraFieldDataAccess : IExtraFieldDataAccess
{
    private readonly ILiteCollection<RecordExtraField> _collection;

    public LiteDbExtraFieldDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<RecordExtraField>("extra_fields");
    }

    public Task<List<RecordExtraField>> GetExtraFieldsForRecordAsync(int recordId)
    {
        var list = _collection.Query()
            .Where(x => x.RecordId == recordId)
            .ToList();

        return Task.FromResult(list);
    }

    public Task<int> SaveExtraFieldAsync(RecordExtraField extraField)
    {
        if (extraField.Id == 0)
        {
            var newId = _collection.Insert(extraField).AsInt32;
            extraField.Id = newId;
            return Task.FromResult(newId);
        }

        _collection.Update(extraField);
        return Task.FromResult(extraField.Id);
    }

    public Task DeleteExtraFieldAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
