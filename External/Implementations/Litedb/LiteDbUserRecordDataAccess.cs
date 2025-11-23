using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.User;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbUserRecordDataAccess : IUserRecordDataAccess
{
    private readonly ILiteCollection<UserData> _collection;

    public LiteDbUserRecordDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<UserData>("users");
    }

    public Task<UserData?> GetUserByIdAsync(int id)
    {
        var result = _collection.FindById(id);
        return Task.FromResult(result);
    }

    public Task<UserData?> GetUserByUserNameAsync(string userName)
    {
        var result = _collection.FindOne(x => x.UserName == userName);
        return Task.FromResult(result);
    }

    public Task<List<UserData>> GetAllUsersAsync()
    {
        var list = _collection.FindAll().ToList();
        return Task.FromResult(list);
    }

    public Task<int> SaveUserAsync(UserData user)
    {
        if (user.Id == 0)
        {
            var newId = _collection.Insert(user).AsInt32;
            user.Id = newId;
            return Task.FromResult(newId);
        }

        _collection.Update(user);
        return Task.FromResult(user.Id);
    }

    public Task<bool> AnyUsersAsync()
    {
        var any = _collection.FindAll().Any();
        return Task.FromResult(any);
    }

    public Task DeleteUserAsync(int id)
    {
        _collection.Delete(id);
        return Task.CompletedTask;
    }
}
