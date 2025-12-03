using Automax.Models.API;
using Automax.Models.OdometerRecord;

namespace Automax.External.Interfaces;

public interface IOdometerRecordDataAccess
{
    Task<OdometerRecord?> GetOdometerRecordAsync(int id);
    Task<List<OdometerRecord>> GetOdometerRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SaveOdometerRecordAsync(OdometerRecord record);
    Task DeleteOdometerRecordAsync(int id);
}
