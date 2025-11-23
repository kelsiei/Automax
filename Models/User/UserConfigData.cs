using CarCareTracker.Models.Settings;

namespace CarCareTracker.Models.User;

public class UserConfigData
{
    public int UserId { get; set; }
    public UserConfig? Config { get; set; }
    // TODO: persistence and versioning to be handled by data access layer.
}
