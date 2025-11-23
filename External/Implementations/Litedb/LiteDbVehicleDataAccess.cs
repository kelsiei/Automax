using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.Vehicle;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbVehicleDataAccess : IVehicleDataAccess
{
    private readonly LiteDBHelper _dbHelper;
    private readonly ILiteCollection<Vehicle> _collection;

    public LiteDbVehicleDataAccess(LiteDBHelper dbHelper)
    {
        _dbHelper = dbHelper;
        _collection = _dbHelper.GetCollection<Vehicle>("vehicles");
    }

    public Task<Vehicle?> GetVehicleAsync(int id)
    {
        var result = _collection.FindById(id);
        return Task.FromResult(result);
    }

    public Task<List<Vehicle>> GetVehiclesAsync(int userId)
    {
        // TODO: Use UserAccess to filter by userId in a later phase.
        var result = _collection.FindAll().ToList();
        return Task.FromResult(result);
    }

    public Task<int> SaveVehicleAsync(Vehicle vehicle)
    {
        if (vehicle.Id == 0)
        {
            var newId = _collection.Insert(vehicle).AsInt32;
            vehicle.Id = newId;
            return Task.FromResult(newId);
        }

        _collection.Update(vehicle);
        return Task.FromResult(vehicle.Id);
    }

    public Task DeleteVehicleAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
