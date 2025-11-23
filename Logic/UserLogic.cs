using CarCareTracker.External.Interfaces;

namespace CarCareTracker.Logic;

public class UserLogic
{
    private readonly IUserAccessDataAccess _userAccessDataAccess;

    public UserLogic(IUserAccessDataAccess userAccessDataAccess)
    {
        _userAccessDataAccess = userAccessDataAccess;
    }

    /// <summary>
    /// Returns a list of vehicle IDs the given user can access.
    /// For root users, an empty list is returned, which callers may interpret as "all vehicles".
    /// </summary>
    public async Task<List<int>> GetAccessibleVehicleIdsForUserAsync(int userId, bool isRootUser)
    {
        if (isRootUser)
        {
            // Root users can access all vehicles; callers interpret empty as "no restriction".
            return new List<int>();
        }

        var accessRecords = await _userAccessDataAccess.GetUserAccessForUserAsync(userId);
        var ids = accessRecords
            .Select(a => a.VehicleId)
            .Distinct()
            .ToList();

        return ids;
    }

    /// <summary>
    /// Determines whether the given user has access to the specified vehicle.
    /// Root users always have access.
    /// </summary>
    public async Task<bool> UserHasAccessToVehicleAsync(int userId, bool isRootUser, int vehicleId)
    {
        if (isRootUser)
        {
            return true;
        }

        var accessRecords = await _userAccessDataAccess.GetUserAccessForUserAsync(userId);
        return accessRecords.Any(a => a.VehicleId == vehicleId);
    }
}
