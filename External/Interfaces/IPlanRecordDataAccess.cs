using Automax.Models.API;
using Automax.Models.PlanRecord;

namespace Automax.External.Interfaces;

public interface IPlanRecordDataAccess
{
    Task<PlanRecord?> GetPlanRecordAsync(int id);
    Task<List<PlanRecord>> GetPlanRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null);
    Task<int> SavePlanRecordAsync(PlanRecord record);
    Task DeletePlanRecordAsync(int id);
}
