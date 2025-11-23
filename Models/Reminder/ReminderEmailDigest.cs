namespace CarCareTracker.Models.Reminder;

public class ReminderEmailDigest
{
    public string UserName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;

    public IList<ReminderCalendarItem> Reminders { get; set; } = new List<ReminderCalendarItem>();
}

