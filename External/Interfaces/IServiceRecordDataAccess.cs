using Automax.Models.API;
using Automax.Models.ServiceRecord;

namespace Automax.External.Interfaces;

public interface IServiceRecordDataAccess
{
    Task<ServiceRecord?> GetServiceRecordAsync(int id);
    Task<List<ServiceRecord>> GetServiceRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SaveServiceRecordAsync(ServiceRecord record);
    Task DeleteServiceRecordAsync(int id);
}
