using CarCareTracker.Helper;
using CarCareTracker.Models.Reminder;
using Xunit;

namespace CarCareTracker.Tests.Helper;

public class ReminderHelperTests
{
    private readonly ReminderHelper _helper = new();

    [Fact]
    public void BuildICalendarFeed_ExcludesCompletedReminders()
    {
        var reminders = new List<ReminderCalendarItem>
        {
            new()
            {
                ReminderId = 1,
                Description = "Completed item",
                DueDate = new DateTime(2025, 1, 1),
                IsCompleted = true
            },
            new()
            {
                ReminderId = 2,
                Description = "Open item",
                DueDate = new DateTime(2025, 1, 2),
                IsCompleted = false
            }
        };

        var ics = _helper.BuildICalendarFeed(reminders, "Test Calendar");

        Assert.Equal(1, CountOccurrences(ics, "BEGIN:VEVENT"));
        Assert.DoesNotContain("Completed item", ics);
        Assert.Contains("Open item", ics);
    }

    [Fact]
    public void BuildICalendarFeed_ExcludesRemindersWithoutDueDate()
    {
        var reminders = new List<ReminderCalendarItem>
        {
            new()
            {
                ReminderId = 1,
                Description = "No date",
                DueDate = null,
                IsCompleted = false
            },
            new()
            {
                ReminderId = 2,
                Description = "Has date",
                DueDate = new DateTime(2025, 1, 3),
                IsCompleted = false
            }
        };

        var ics = _helper.BuildICalendarFeed(reminders, "Test Calendar");

        Assert.Equal(1, CountOccurrences(ics, "BEGIN:VEVENT"));
        Assert.DoesNotContain("No date", ics);
        Assert.Contains("Has date", ics);
    }

    [Fact]
    public void BuildICalendarFeed_UsesAllDayDates()
    {
        var reminders = new List<ReminderCalendarItem>
        {
            new()
            {
                ReminderId = 1,
                Description = "Date check",
                DueDate = new DateTime(2025, 1, 2),
                IsCompleted = false
            }
        };

        var ics = _helper.BuildICalendarFeed(reminders, "Test Calendar");

        Assert.Contains("DTSTART;VALUE=DATE:20250102", ics);
        Assert.Contains("DTEND;VALUE=DATE:20250102", ics);
    }

    [Fact]
    public void BuildICalendarFeed_EscapesSpecialCharacters()
    {
        var reminders = new List<ReminderCalendarItem>
        {
            new()
            {
                ReminderId = 1,
                Description = "Desc, with; specials\\and\nnewlines",
                Tags = "tag1,tag2;tag3",
                DueDate = new DateTime(2025, 2, 1),
                IsCompleted = false,
                TargetOdometer = 120000
            }
        };

        var ics = _helper.BuildICalendarFeed(reminders, "Test Calendar");

        Assert.Contains("SUMMARY:Desc\\, with\\; specials\\\\and\\nnewlines", ics);
        Assert.Contains("DESCRIPTION:Desc\\, with\\; specials\\\\and\\nnewlines Tags: tag1\\,tag2\\;tag3 Target odometer: 120000", ics);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
