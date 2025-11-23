using CarCareTracker.Models.API;
using CarCareTracker.Models.PlanRecord;

namespace CarCareTracker.External.Interfaces;

public interface IPlanRecordDataAccess
{
    Task<PlanRecord?> GetPlanRecordAsync(int id);
    Task<List<PlanRecord>> GetPlanRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SavePlanRecordAsync(PlanRecord record);
    Task DeletePlanRecordAsync(int id);
}
