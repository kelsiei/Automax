namespace CarCareTracker.Models.Settings;

public class ReminderUrgencyConfig
{
    public int DaysUntilUrgent { get; set; }
    public int DaysUntilVeryUrgent { get; set; }
    public int DaysUntilPastDue { get; set; }
    // TODO: add mileage-based thresholds and custom colors to fully match spec.
}
