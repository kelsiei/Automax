using CarCareTracker.Enum;

namespace CarCareTracker.Models.Settings;

public class UserConfig
{
    public string? DefaultTab { get; set; }
    public List<string> VisibleTabs { get; set; } = new();
    public bool HideSoldVehicles { get; set; }
    public string? DistanceUnit { get; set; }
    public string? FuelEconomyUnit { get; set; }
    public string? CurrencySymbol { get; set; }
    public ReminderUrgencyConfig? ReminderUrgencyConfig { get; set; }
    public List<UserColumnPreference> ColumnPreferences { get; set; } = new();
    public List<DashboardMetric> DashboardMetrics { get; set; } = new();
    // TODO: add tab-specific preferences and allowed tabs per spec.
}
