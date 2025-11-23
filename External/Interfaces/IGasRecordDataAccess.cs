using CarCareTracker.Models.API;
using CarCareTracker.Models.GasRecord;

namespace CarCareTracker.External.Interfaces;

public interface IGasRecordDataAccess
{
    Task<GasRecord?> GetGasRecordAsync(int id);
    Task<List<GasRecord>> GetGasRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SaveGasRecordAsync(GasRecord record);
    Task DeleteGasRecordAsync(int id);
}
