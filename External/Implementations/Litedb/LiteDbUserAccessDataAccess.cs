using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.User;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbUserAccessDataAccess : IUserAccessDataAccess
{
    private readonly ILiteCollection<UserAccess> _collection;

    public LiteDbUserAccessDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<UserAccess>("user_access");
    }

    public Task<List<UserAccess>> GetUserAccessForUserAsync(int userId)
    {
        var list = _collection.Query()
            .Where(x => x.UserId == userId)
            .ToList();

        return Task.FromResult(list);
    }

    public Task<List<UserAccess>> GetUserAccessForVehicleAsync(int vehicleId)
    {
        var list = _collection.Query()
            .Where(x => x.VehicleId == vehicleId)
            .ToList();

        return Task.FromResult(list);
    }

    public Task<int> SaveUserAccessAsync(UserAccess access)
    {
        if (access.Id == 0)
        {
            var newId = _collection.Insert(access).AsInt32;
            access.Id = newId;
            return Task.FromResult(newId);
        }

        _collection.Update(access);
        return Task.FromResult(access.Id);
    }

    public Task DeleteUserAccessAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
