using System.Text;
using CarCareTracker.Models.Reminder;

namespace CarCareTracker.Helper;

public class ReminderHelper
{
    public string BuildICalendarFeed(IEnumerable<ReminderCalendarItem> reminders, string calendarName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("BEGIN:VCALENDAR");
        sb.AppendLine("PRODID:-//CarCareTracker//EN");
        sb.AppendLine("VERSION:2.0");
        sb.AppendLine($"NAME:{EscapeText(calendarName)}");
        sb.AppendLine($"X-WR-CALNAME:{EscapeText(calendarName)}");

        foreach (var r in reminders)
        {
            if (!r.DueDate.HasValue || r.IsCompleted)
            {
                continue;
            }

            var date = r.DueDate.Value.Date;
            var dt = date.ToString("yyyyMMdd");

            var uid = $"carcare-{r.ReminderId}@carcaretracker";
            var summary = $"{r.Year} {r.Make} {r.Model} ({r.LicensePlate}): {r.Description}";
            var descriptionBuilder = new StringBuilder();
            descriptionBuilder.Append(r.Description ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(r.Tags))
            {
                if (descriptionBuilder.Length > 0)
                {
                    descriptionBuilder.Append(" ");
                }
                descriptionBuilder.Append($"Tags: {r.Tags}");
            }

            if (r.TargetOdometer.HasValue)
            {
                if (descriptionBuilder.Length > 0)
                {
                    descriptionBuilder.Append(" ");
                }
                descriptionBuilder.Append($"Target odometer: {r.TargetOdometer.Value}");
            }

            sb.AppendLine("BEGIN:VEVENT");
            sb.AppendLine($"UID:{EscapeText(uid)}");
            sb.AppendLine($"SUMMARY:{EscapeText(summary)}");
            sb.AppendLine($"DTSTART;VALUE=DATE:{dt}");
            sb.AppendLine($"DTEND;VALUE=DATE:{dt}");
            sb.AppendLine($"DESCRIPTION:{EscapeText(descriptionBuilder.ToString())}");
            sb.AppendLine("END:VEVENT");
        }

        sb.AppendLine("END:VCALENDAR");

        return sb.ToString();
    }

    private static string EscapeText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value
            .Replace(@"\", @"\\")
            .Replace(";", @"\;")
            .Replace(",", @"\,")
            .Replace("\r\n", @"\n")
            .Replace("\n", @"\n");

        return escaped;
    }
}
