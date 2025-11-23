using System.Security.Claims;
using CarCareTracker.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class BackupController : Controller
{
    private readonly ILogger<BackupController> _logger;
    private readonly BackupHelper _backupHelper;

    public BackupController(
        ILogger<BackupController> logger,
        BackupHelper backupHelper)
    {
        _logger = logger;
        _backupHelper = backupHelper;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var (_, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        return View();
    }

    [HttpGet]
    public IActionResult Download()
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        var (content, fileName) = _backupHelper.CreateLiteDbBackupZip();

        _logger.LogInformation("User {UserId} downloaded a backup archive.", userId?.ToString() ?? "unknown");

        return File(content, "application/zip", fileName);
    }

    private (int? UserId, bool IsRootUser) GetCurrentUserContext()
    {
        int? userId = null;
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (idClaim != null && int.TryParse(idClaim.Value, out var parsed))
        {
            userId = parsed;
        }

        var isRootClaim = User.FindFirst("IsRootUser");
        var isRootUser = string.Equals(isRootClaim?.Value, "true", StringComparison.OrdinalIgnoreCase);

        return (userId, isRootUser);
    }
}

