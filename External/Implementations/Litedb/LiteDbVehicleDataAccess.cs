using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Models.Vehicle;
using LiteDB;

namespace Automax.External.Implementations.Litedb;

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
        return Task.FromResult<Vehicle?>(result);
    }

    public Task<List<Vehicle>> GetVehiclesAsync(int userId, bool isRootUser, IEnumerable<int>? allowedVehicleIds)
    {
        var result = _collection.FindAll().ToList();

        if (isRootUser)
        {
            return Task.FromResult(result);
        }

        if (allowedVehicleIds == null)
        {
            return Task.FromResult(new List<Vehicle>());
        }

        var allowedSet = new HashSet<int>(allowedVehicleIds);
        var filtered = result.Where(v => allowedSet.Contains(v.Id)).ToList();
        return Task.FromResult(filtered);
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
