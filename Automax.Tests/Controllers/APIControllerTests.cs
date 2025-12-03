using System.Security.Claims;
using System.Text;
using Automax.Controllers;
using Automax.Helper;
using Automax.Logic;
using Automax.Models.Reminder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Automax.Tests.Controllers;

public class APIControllerTests
{
    [Fact]
    public async Task GetCalendar_ReturnsIcsFile_ForAuthenticatedUser()
    {
        var reminder = new ReminderCalendarItem
        {
            ReminderId = 1,
            VehicleId = 1,
            Year = 2020,
            Make = "Test",
            Model = "Car",
            LicensePlate = "ABC123",
            Description = "Test reminder",
            DueDate = DateTime.UtcNow.Date
        };

        var reminderLogic = new Mock<ReminderLogic>(MockBehavior.Strict, null!, null!, null!);
        reminderLogic.Setup(r => r.GetDateBasedRemindersForUserAsync(1, false))
            .ReturnsAsync(new List<ReminderCalendarItem> { reminder });

        var vehicleLogic = new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!);
        var userLogic = new Mock<UserLogic>(MockBehavior.Strict, null!);
        var backupHelper = new Mock<BackupHelper>(MockBehavior.Strict, null!);
        var controller = new APIController(
            Mock.Of<ILogger<APIController>>(),
            vehicleLogic.Object,
            userLogic.Object,
            reminderLogic.Object,
            new ReminderHelper(),
            backupHelper.Object);

        controller.ControllerContext = BuildContext(userId: "1", isRootUser: false);

        var result = await controller.GetCalendar();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/calendar; charset=utf-8", file.ContentType);
        var content = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("BEGIN:VCALENDAR", content);
        Assert.Contains("Test reminder", content);
        reminderLogic.Verify(r => r.GetDateBasedRemindersForUserAsync(1, false), Times.Once);
    }

    [Fact]
    public void DownloadBackup_RootUser_ReturnsZip()
    {
        var backupBytes = new byte[] { 1, 2, 3 };
        var backupHelper = new Mock<BackupHelper>(MockBehavior.Strict, Mock.Of<ILogger<BackupHelper>>());
        backupHelper.Setup(b => b.CreateLiteDbBackupZip()).Returns((backupBytes, "automax-backup.zip"));

        var controller = new APIController(
            Mock.Of<ILogger<APIController>>(),
            new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!).Object,
            new Mock<UserLogic>(MockBehavior.Strict, null!).Object,
            new Mock<ReminderLogic>(MockBehavior.Strict, null!, null!, null!).Object,
            new ReminderHelper(),
            backupHelper.Object);

        controller.ControllerContext = BuildContext(userId: "1", isRootUser: true);

        var result = controller.DownloadBackup();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Equal("automax-backup.zip", file.FileDownloadName);
        Assert.Equal(backupBytes, file.FileContents);
        backupHelper.Verify(b => b.CreateLiteDbBackupZip(), Times.Once);
    }

    [Fact]
    public void DownloadBackup_NonRoot_Forbid()
    {
        var backupHelper = new Mock<BackupHelper>(MockBehavior.Strict, Mock.Of<ILogger<BackupHelper>>());
        var controller = new APIController(
            Mock.Of<ILogger<APIController>>(),
            new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!).Object,
            new Mock<UserLogic>(MockBehavior.Strict, null!).Object,
            new Mock<ReminderLogic>(MockBehavior.Strict, null!, null!, null!).Object,
            new ReminderHelper(),
            backupHelper.Object);

        controller.ControllerContext = BuildContext(userId: "1", isRootUser: false);

        var result = controller.DownloadBackup();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RestoreBackup_RootUser_Succeeds()
    {
        var backupHelper = new Mock<BackupHelper>(MockBehavior.Strict, Mock.Of<ILogger<BackupHelper>>());
        backupHelper.Setup(b => b.RestoreLiteDbBackupAsync(It.IsAny<Stream>())).ReturnsAsync(true);

        var controller = new APIController(
            Mock.Of<ILogger<APIController>>(),
            new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!).Object,
            new Mock<UserLogic>(MockBehavior.Strict, null!).Object,
            new Mock<ReminderLogic>(MockBehavior.Strict, null!, null!, null!).Object,
            new ReminderHelper(),
            backupHelper.Object);

        controller.ControllerContext = BuildContext(userId: "1", isRootUser: true);

        var content = new byte[] { 1, 2, 3 };
        await using var ms = new MemoryStream(content);
        var formFile = new FormFile(ms, 0, content.Length, "file", "backup.zip");

        var result = await controller.RestoreBackup(formFile);

        Assert.IsType<OkResult>(result);
        backupHelper.Verify(b => b.RestoreLiteDbBackupAsync(It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public async Task RestoreBackup_NonRoot_Forbid()
    {
        var backupHelper = new Mock<BackupHelper>(MockBehavior.Strict, Mock.Of<ILogger<BackupHelper>>());
        var controller = new APIController(
            Mock.Of<ILogger<APIController>>(),
            new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!).Object,
            new Mock<UserLogic>(MockBehavior.Strict, null!).Object,
            new Mock<ReminderLogic>(MockBehavior.Strict, null!, null!, null!).Object,
            new ReminderHelper(),
            backupHelper.Object);

        controller.ControllerContext = BuildContext(userId: "1", isRootUser: false);

        var result = await controller.RestoreBackup(null);

        Assert.IsType<ForbidResult>(result);
        backupHelper.Verify(b => b.RestoreLiteDbBackupAsync(It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task RestoreBackup_InvalidFile_BadRequest()
    {
        var backupHelper = new Mock<BackupHelper>(MockBehavior.Strict, Mock.Of<ILogger<BackupHelper>>());
        var controller = new APIController(
            Mock.Of<ILogger<APIController>>(),
            new Mock<VehicleLogic>(MockBehavior.Strict, null!, null!, null!, null!, null!, null!, null!, null!).Object,
            new Mock<UserLogic>(MockBehavior.Strict, null!).Object,
            new Mock<ReminderLogic>(MockBehavior.Strict, null!, null!, null!).Object,
            new ReminderHelper(),
            backupHelper.Object);

        controller.ControllerContext = BuildContext(userId: "1", isRootUser: true);

        var emptyStream = new MemoryStream();
        var formFile = new FormFile(emptyStream, 0, 0, "file", "backup.zip");

        var result = await controller.RestoreBackup(formFile);

        Assert.IsType<BadRequestObjectResult>(result);
        backupHelper.Verify(b => b.RestoreLiteDbBackupAsync(It.IsAny<Stream>()), Times.Never);
    }

    private static ControllerContext BuildContext(string userId, bool isRootUser)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("IsRootUser", isRootUser ? "true" : "false")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
