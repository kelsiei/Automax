using System.Collections.Generic;

namespace Automax.Models.Reminder;

public class ReminderEmailViewModel
{
    public bool EnableReminderEmails { get; set; }
    public int? ReminderEmailDaysAhead { get; set; }
    public IEnumerable<ReminderEmailDigest> Digests { get; set; } = new List<ReminderEmailDigest>();
}
