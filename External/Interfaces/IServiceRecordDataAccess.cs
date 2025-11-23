using CarCareTracker.Models.API;
using CarCareTracker.Models.ServiceRecord;

namespace CarCareTracker.External.Interfaces;

public interface IServiceRecordDataAccess
{
    Task<ServiceRecord?> GetServiceRecordAsync(int id);
    Task<List<ServiceRecord>> GetServiceRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SaveServiceRecordAsync(ServiceRecord record);
    Task DeleteServiceRecordAsync(int id);
}
