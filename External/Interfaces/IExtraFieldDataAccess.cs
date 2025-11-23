using CarCareTracker.Models.Shared;

namespace CarCareTracker.External.Interfaces;

public interface IExtraFieldDataAccess
{
    Task<List<RecordExtraField>> GetExtraFieldsForRecordAsync(int recordId);
    Task<int> SaveExtraFieldAsync(RecordExtraField extraField);
    Task DeleteExtraFieldAsync(int id);
}
