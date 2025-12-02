using Automax.Models.Settings;

namespace Automax.Models.User;

public class UserConfigData
{
    public int UserId { get; set; }
    public UserConfig? Config { get; set; }
    // TODO: persistence and versioning to be handled by data access layer.
}
