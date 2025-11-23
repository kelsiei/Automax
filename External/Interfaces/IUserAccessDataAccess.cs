using CarCareTracker.Models.User;

namespace CarCareTracker.External.Interfaces;

public interface IUserAccessDataAccess
{
    Task<List<UserAccess>> GetUserAccessForUserAsync(int userId);
    Task<List<UserAccess>> GetUserAccessForVehicleAsync(int vehicleId);

    Task<int> SaveUserAccessAsync(UserAccess access);
    Task DeleteUserAccessAsync(int id);
}
