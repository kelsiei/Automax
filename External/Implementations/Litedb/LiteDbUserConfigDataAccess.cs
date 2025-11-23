using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.Settings;
using CarCareTracker.Models.User;
using LiteDB;

namespace CarCareTracker.External.Implementations.Litedb;

public class LiteDbUserConfigDataAccess : IUserConfigDataAccess
{
    private readonly ILiteCollection<UserConfigData> _collection;

    public LiteDbUserConfigDataAccess(LiteDBHelper dbHelper)
    {
        _collection = dbHelper.GetCollection<UserConfigData>("user_configs");
    }

    public Task<UserConfigData?> GetUserConfigAsync(int userId)
    {
        var result = _collection.FindOne(x => x.UserId == userId);
        return Task.FromResult(result);
    }

    public Task SaveUserConfigAsync(int userId, UserConfig config)
    {
        var existing = _collection.FindOne(x => x.UserId == userId);

        if (existing == null)
        {
            existing = new UserConfigData
            {
                UserId = userId,
                Config = config
            };
            _collection.Insert(existing);
        }
        else
        {
            existing.Config = config;
            _collection.Update(existing);
        }

        return Task.CompletedTask;
    }
}
