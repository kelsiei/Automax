using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Logic;
using CarCareTracker.Models.Reminder;
using CarCareTracker.Models.Settings;
using CarCareTracker.Models.User;
using CarCareTracker.Models.Vehicle;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.IO;

namespace CarCareTracker.Tests.Logic;

public class ReminderEmailLogicTests : IDisposable
{
    private readonly string _configDir = StaticHelper.ConfigDirectory;

    public ReminderEmailLogicTests()
    {
        Cleanup();
    }

    [Fact]
    public async Task BuildReminderEmailDigestsAsync_EmailsDisabled_ReturnsEmpty()
    {
        var (logic, reminderLogic, userDataAccess, configHelper) = BuildLogicWithMocks();
        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = false });

        userDataAccess.Setup(u => u.GetAllUsersAsync()).ReturnsAsync(new List<UserData>
        {
            new() { Id = 1, EmailAddress = "u@example.com", UserName = "user" }
        });
        reminderLogic.Setup(r => r.GetDateBasedRemindersForUserAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<ReminderCalendarItem>
            {
                new() { ReminderId = 1, DueDate = DateTime.UtcNow.Date, IsCompleted = false }
            });

        var result = await logic.BuildReminderEmailDigestsAsync();

        Assert.Empty(result);
        reminderLogic.Verify(r => r.GetDateBasedRemindersForUserAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task BuildReminderEmailDigestsAsync_NoUsersWithEmail_ReturnsEmpty()
    {
        var (logic, reminderLogic, userDataAccess, configHelper) = BuildLogicWithMocks();
        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = true });

        userDataAccess.Setup(u => u.GetAllUsersAsync()).ReturnsAsync(new List<UserData>
        {
            new() { Id = 1, EmailAddress = "", UserName = "user" }
        });
        reminderLogic.Setup(r => r.GetDateBasedRemindersForUserAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<ReminderCalendarItem>
            {
                new() { ReminderId = 1, DueDate = DateTime.UtcNow.Date, IsCompleted = false }
            });

        var result = await logic.BuildReminderEmailDigestsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task BuildReminderEmailDigestsAsync_FiltersByWindowAndOpenReminders()
    {
        var today = DateTime.UtcNow.Date;
        var (logic, reminderLogic, userDataAccess, configHelper) = BuildLogicWithMocks();
        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = true, ReminderEmailDaysAhead = 7 });

        userDataAccess.Setup(u => u.GetAllUsersAsync()).ReturnsAsync(new List<UserData>
        {
            new() { Id = 1, EmailAddress = "u@example.com", UserName = "user" }
        });

        reminderLogic.Setup(r => r.GetDateBasedRemindersForUserAsync(1, It.IsAny<bool>()))
            .ReturnsAsync(new List<ReminderCalendarItem>
            {
                new() { ReminderId = 1, DueDate = today.AddDays(3), IsCompleted = false, Description = "Inside" },
                new() { ReminderId = 2, DueDate = today.AddDays(-1), IsCompleted = false, Description = "Past" },
                new() { ReminderId = 3, DueDate = today.AddDays(10), IsCompleted = false, Description = "Future" },
                new() { ReminderId = 4, DueDate = today.AddDays(2), IsCompleted = true, Description = "Completed" },
                new() { ReminderId = 5, DueDate = null, IsCompleted = false, Description = "No date" }
            });

        var result = await logic.BuildReminderEmailDigestsAsync();

        var digest = Assert.Single(result);
        var reminders = digest.Reminders;
        Assert.Single(reminders);
        Assert.Equal("Inside", reminders[0].Description);
    }

    [Fact]
    public async Task BuildReminderEmailDigestsAsync_GroupsPerUserAndOrdersReminders()
    {
        var today = DateTime.UtcNow.Date;
        var (logic, reminderLogic, userDataAccess, configHelper) = BuildLogicWithMocks();
        configHelper.SaveServerConfig(new ServerConfig { EnableReminderEmails = true, ReminderEmailDaysAhead = 10 });

        userDataAccess.Setup(u => u.GetAllUsersAsync()).ReturnsAsync(new List<UserData>
        {
            new() { Id = 1, EmailAddress = "a@example.com", UserName = "A" },
            new() { Id = 2, EmailAddress = "b@example.com", UserName = "B" }
        });

        reminderLogic.Setup(r => r.GetDateBasedRemindersForUserAsync(1, It.IsAny<bool>()))
            .ReturnsAsync(new List<ReminderCalendarItem>
            {
                new() { ReminderId = 1, DueDate = today.AddDays(5), IsCompleted = false, Year = 2020, Make = "Make1", Model = "Model2", LicensePlate = "B", Description = "Desc2" },
                new() { ReminderId = 2, DueDate = today.AddDays(5), IsCompleted = false, Year = 2020, Make = "Make1", Model = "Model1", LicensePlate = "A", Description = "Desc1" }
            });

        reminderLogic.Setup(r => r.GetDateBasedRemindersForUserAsync(2, It.IsAny<bool>()))
            .ReturnsAsync(new List<ReminderCalendarItem>
            {
                new() { ReminderId = 3, DueDate = today.AddDays(1), IsCompleted = false, Year = 2021, Make = "X", Model = "Y", LicensePlate = "Z", Description = "Other" }
            });

        var result = await logic.BuildReminderEmailDigestsAsync();

        Assert.Equal(2, result.Count);
        var userA = result.Single(d => d.EmailAddress == "a@example.com");
        var ordered = userA.Reminders.ToList();
        Assert.Equal(2, ordered.Count);
        // Order by DueDate then Make/Model/Plate/Description
        Assert.Equal("Desc1", ordered[0].Description);
        Assert.Equal("Desc2", ordered[1].Description);

        var userB = result.Single(d => d.EmailAddress == "b@example.com");
        Assert.Single(userB.Reminders);
        Assert.Equal("Other", userB.Reminders[0].Description);
    }

    private (ReminderEmailLogic logic, Mock<ReminderLogic> reminderLogic, Mock<IUserRecordDataAccess> userDataAccess, ConfigHelper configHelper) BuildLogicWithMocks()
    {
        var reminderLogic = new Mock<ReminderLogic>(MockBehavior.Strict, null!, null!, null!);
        var userDataAccess = new Mock<IUserRecordDataAccess>();
        var configLogger = Mock.Of<ILogger<ConfigHelper>>();
        var configHelper = new ConfigHelper(configLogger);

        var logic = new ReminderEmailLogic(configHelper, reminderLogic.Object, userDataAccess.Object);
        return (logic, reminderLogic, userDataAccess, configHelper);
    }

    public void Dispose()
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (Directory.Exists(_configDir))
        {
            Directory.Delete(_configDir, recursive: true);
        }
    }
}
