using CarCareTracker.Models.API;
using CarCareTracker.Models.OdometerRecord;

namespace CarCareTracker.External.Interfaces;

public interface IOdometerRecordDataAccess
{
    Task<OdometerRecord?> GetOdometerRecordAsync(int id);
    Task<List<OdometerRecord>> GetOdometerRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SaveOdometerRecordAsync(OdometerRecord record);
    Task DeleteOdometerRecordAsync(int id);
}
