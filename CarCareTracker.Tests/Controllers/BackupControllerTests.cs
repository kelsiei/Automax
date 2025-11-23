using System.Security.Claims;
using CarCareTracker.Controllers;
using CarCareTracker.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarCareTracker.Tests.Controllers;

public class BackupControllerTests
{
    [Fact]
    public void Index_NonRoot_Forbid()
    {
        var controller = BuildController();
        controller.ControllerContext = BuildContext(isRootUser: false);

        var result = controller.Index();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void Index_Root_View()
    {
        var controller = BuildController();
        controller.ControllerContext = BuildContext(isRootUser: true);

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Download_NonRoot_Forbid()
    {
        var controller = BuildController();
        controller.ControllerContext = BuildContext(isRootUser: false);

        var result = controller.Download();

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void Download_Root_FileResult()
    {
        var backupHelper = new Mock<BackupHelper>(MockBehavior.Strict, Mock.Of<ILogger<BackupHelper>>());
        backupHelper.Setup(b => b.CreateLiteDbBackupZip()).Returns((new byte[] { 1, 2, 3 }, "test.zip"));

        var controller = new BackupController(Mock.Of<ILogger<BackupController>>(), backupHelper.Object);
        controller.ControllerContext = BuildContext(isRootUser: true);

        var result = controller.Download();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Equal("test.zip", file.FileDownloadName);
        Assert.Equal(new byte[] { 1, 2, 3 }, file.FileContents);
        backupHelper.Verify(b => b.CreateLiteDbBackupZip(), Times.Once);
    }

    private BackupController BuildController()
    {
        return new BackupController(Mock.Of<ILogger<BackupController>>(), Mock.Of<BackupHelper>(Mock.Of<ILogger<BackupHelper>>()));
    }

    private ControllerContext BuildContext(bool isRootUser)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "1") };
        if (isRootUser)
        {
            claims.Add(new Claim("IsRootUser", "true"));
        }
        else
        {
            claims.Add(new Claim("IsRootUser", "false"));
        }

        var identity = new ClaimsIdentity(claims, "Test");
        var user = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}
