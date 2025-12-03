using Automax.Models.User;

namespace Automax.External.Interfaces;

public interface IUserRecordDataAccess
{
    Task<UserData?> GetUserByIdAsync(int id);
    Task<UserData?> GetUserByUserNameAsync(string userName);
    Task<List<UserData>> GetAllUsersAsync();
    Task<int> SaveUserAsync(UserData user);
    Task<bool> AnyUsersAsync();
    Task DeleteUserAsync(int id);
}
