using CarCareTracker.Models.Settings;
using CarCareTracker.Models.User;

namespace CarCareTracker.External.Interfaces;

public interface IUserConfigDataAccess
{
    Task<UserConfigData?> GetUserConfigAsync(int userId);
    Task SaveUserConfigAsync(int userId, UserConfig config);
}
