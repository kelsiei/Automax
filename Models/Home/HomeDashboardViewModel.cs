using CarCareTracker.Models.Reminder;

namespace CarCareTracker.Models.Home;

public class HomeDashboardViewModel
{
    public string Motd { get; set; } = string.Empty;

    public int VehicleCount { get; set; }
    public int VehiclesWithUrgentReminders { get; set; }
    public int OpenRemindersCount { get; set; }

    public IList<ReminderCalendarItem> UpcomingReminders { get; set; } = new List<ReminderCalendarItem>();

    public bool ShowMotd => !string.IsNullOrWhiteSpace(Motd);

    public bool IsAuthenticated { get; set; }

    public string UserName { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public bool IsRootUser { get; set; }
}

