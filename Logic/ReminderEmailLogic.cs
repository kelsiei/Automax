using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.Reminder;

namespace CarCareTracker.Logic;

public class ReminderEmailLogic
{
    private readonly ConfigHelper _configHelper;
    private readonly ReminderLogic _reminderLogic;
    private readonly IUserRecordDataAccess _userRecordDataAccess;

    public ReminderEmailLogic(
        ConfigHelper configHelper,
        ReminderLogic reminderLogic,
        IUserRecordDataAccess userRecordDataAccess)
    {
        _configHelper = configHelper;
        _reminderLogic = reminderLogic;
        _userRecordDataAccess = userRecordDataAccess;
    }

    public async Task<IList<ReminderEmailDigest>> BuildReminderEmailDigestsAsync()
    {
        var serverConfig = _configHelper.LoadServerConfig();
        if (!serverConfig.EnableReminderEmails)
        {
            return new List<ReminderEmailDigest>();
        }

        var daysAhead = serverConfig.ReminderEmailDaysAhead.HasValue && serverConfig.ReminderEmailDaysAhead.Value > 0
            ? serverConfig.ReminderEmailDaysAhead.Value
            : 7;

        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(daysAhead);

        var users = await _userRecordDataAccess.GetAllUsersAsync();
        var digests = new List<ReminderEmailDigest>();

        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.EmailAddress))
            {
                continue;
            }

            var reminders = await _reminderLogic.GetDateBasedRemindersForUserAsync(user.Id, user.IsRootUser);

            var selected = reminders
                .Where(r =>
                    !r.IsCompleted &&
                    r.DueDate.HasValue &&
                    r.DueDate.Value.Date >= today &&
                    r.DueDate.Value.Date <= cutoff)
                .OrderBy(r => r.DueDate!.Value.Date)
                .ThenBy(r => r.Year)
                .ThenBy(r => r.Make)
                .ThenBy(r => r.Model)
                .ThenBy(r => r.LicensePlate)
                .ThenBy(r => r.Description)
                .ToList();

            if (selected.Count == 0)
            {
                continue;
            }

            var displayName = string.IsNullOrWhiteSpace(user.UserName)
                ? user.EmailAddress
                : user.UserName;

            digests.Add(new ReminderEmailDigest
            {
                UserName = displayName,
                EmailAddress = user.EmailAddress ?? string.Empty,
                Reminders = selected
            });
        }

        return digests;
    }
}

