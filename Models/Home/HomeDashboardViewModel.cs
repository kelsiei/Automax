using Automax.Models.Reminder;

namespace Automax.Models.Home;

public class HomeDashboardViewModel
{
    public string Motd { get; set; } = string.Empty;

    public int VehicleCount { get; set; }
    public int VehiclesWithUrgentReminders { get; set; }
    public int OpenRemindersCount { get; set; }

    public IList<ReminderCalendarItem> UpcomingReminders { get; set; } = new List<ReminderCalendarItem>();
    public IList<ServiceSummaryItem> RecentServices { get; set; } = new List<ServiceSummaryItem>();
    public IList<FuelSummaryItem> FuelSummaries { get; set; } = new List<FuelSummaryItem>();
    public bool ShowFuelWidget { get; set; } = true;
    public string PreferredUnits { get; set; } = string.Empty;

    public bool ShowMotd => !string.IsNullOrWhiteSpace(Motd);

    public bool IsAuthenticated { get; set; }

    public string UserName { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    public bool IsRootUser { get; set; }
}

public class ServiceSummaryItem
{
    public int VehicleId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? Cost { get; set; }
}

public class FuelSummaryItem
{
    public int VehicleId { get; set; }
    public decimal AverageEfficiency { get; set; }
    public decimal TotalFuelCost { get; set; }
}

