using Automax.Models.Settings;
using Automax.Models.User;

namespace Automax.External.Interfaces;

public interface IUserConfigDataAccess
{
    Task<UserConfigData?> GetUserConfigAsync(int userId);
    Task SaveUserConfigAsync(int userId, UserConfig config);
}
