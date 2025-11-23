namespace CarCareTracker.Models.API;

public class ReleaseVersion
{
    public string CurrentVersion { get; set; } = string.Empty;
    public string? LatestVersion { get; set; }
    public bool HasUpdate { get; set; }
}
