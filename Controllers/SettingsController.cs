using System.Security.Claims;
using CarCareTracker.Helper;
using CarCareTracker.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly ILogger<SettingsController> _logger;
    private readonly ConfigHelper _configHelper;

    public SettingsController(
        ILogger<SettingsController> logger,
        ConfigHelper configHelper)
    {
        _logger = logger;
        _configHelper = configHelper;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        var serverConfig = _configHelper.LoadServerConfig();

        var vm = new ServerSettingsViewModel
        {
            Motd = serverConfig.Motd ?? string.Empty,
            EnableAuth = serverConfig.EnableAuth,
            LocaleOverride = serverConfig.LocaleOverride ?? string.Empty,
            LocaleDateTimeOverride = serverConfig.LocaleDateTimeOverride ?? string.Empty,
            MaxDocumentUploadSizeMb = serverConfig.MaxDocumentUploadBytes.HasValue && serverConfig.MaxDocumentUploadBytes.Value > 0
                ? (int?)(serverConfig.MaxDocumentUploadBytes.Value / (1024 * 1024))
                : null,
            EnableReminderEmails = serverConfig.EnableReminderEmails,
            ReminderEmailDaysAhead = serverConfig.ReminderEmailDaysAhead
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(ServerSettingsViewModel model)
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var serverConfig = _configHelper.LoadServerConfig();

        serverConfig.Motd = string.IsNullOrWhiteSpace(model.Motd)
            ? null
            : model.Motd.Trim();
        serverConfig.EnableAuth = model.EnableAuth;
        serverConfig.LocaleOverride = string.IsNullOrWhiteSpace(model.LocaleOverride)
            ? null
            : model.LocaleOverride.Trim();
        serverConfig.LocaleDateTimeOverride = string.IsNullOrWhiteSpace(model.LocaleDateTimeOverride)
            ? null
            : model.LocaleDateTimeOverride.Trim();
        serverConfig.MaxDocumentUploadBytes = model.MaxDocumentUploadSizeMb.HasValue
            ? model.MaxDocumentUploadSizeMb.Value * 1024L * 1024L
            : null;
        serverConfig.EnableReminderEmails = model.EnableReminderEmails;
        serverConfig.ReminderEmailDaysAhead = model.ReminderEmailDaysAhead.HasValue && model.ReminderEmailDaysAhead.Value > 0
            ? model.ReminderEmailDaysAhead.Value
            : null;

        _configHelper.SaveServerConfig(serverConfig);

        _logger.LogInformation("Server settings updated by root user {UserId}.", userId);

        TempData["StatusMessage"] = "Server settings have been updated.";
        return RedirectToAction(nameof(Index));
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
