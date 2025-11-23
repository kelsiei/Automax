using CarCareTracker.Models.Vehicle;

namespace CarCareTracker.External.Interfaces;

public interface IVehicleDataAccess
{
    Task<Vehicle?> GetVehicleAsync(int id);
    Task<List<Vehicle>> GetVehiclesAsync(int userId);
    Task<int> SaveVehicleAsync(Vehicle vehicle);
    Task DeleteVehicleAsync(int id);
}
