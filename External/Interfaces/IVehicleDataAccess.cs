using Automax.Models.Vehicle;

namespace Automax.External.Interfaces;

public interface IVehicleDataAccess
{
    Task<Vehicle?> GetVehicleAsync(int id);
    Task<List<Vehicle>> GetVehiclesAsync(int userId, bool isRootUser, IEnumerable<int>? allowedVehicleIds);
    Task<int> SaveVehicleAsync(Vehicle vehicle);
    Task DeleteVehicleAsync(int id);
}
