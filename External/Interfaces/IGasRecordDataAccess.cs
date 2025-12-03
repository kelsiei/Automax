using Automax.Models.API;
using Automax.Models.GasRecord;

namespace Automax.External.Interfaces;

public interface IGasRecordDataAccess
{
    Task<GasRecord?> GetGasRecordAsync(int id);
    Task<List<GasRecord>> GetGasRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SaveGasRecordAsync(GasRecord record);
    Task DeleteGasRecordAsync(int id);
}
