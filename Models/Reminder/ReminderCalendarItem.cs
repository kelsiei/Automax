using CarCareTracker.Enum;

namespace CarCareTracker.Models.Reminder;

public class ReminderCalendarItem
{
    public int ReminderId { get; set; }
    public int VehicleId { get; set; }
    public int Year { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public ReminderUrgency Urgency { get; set; }
    public string Tags { get; set; } = string.Empty;
    public int? TargetOdometer { get; set; }
}
