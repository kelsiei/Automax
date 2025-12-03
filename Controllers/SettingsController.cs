using System.Security.Claims;
using Automax.Helper;
using Automax.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automax.Controllers;

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
            OpenRegistration = serverConfig.OpenRegistration,
            DisableRegistration = serverConfig.DisableRegistration,
            EnableRootUserOidc = serverConfig.EnableRootUserOidc,
            LocaleOverride = serverConfig.LocaleOverride ?? string.Empty,
            LocaleDateTimeOverride = serverConfig.LocaleDateTimeOverride ?? string.Empty,
            MaxDocumentUploadSizeMb = serverConfig.MaxDocumentUploadBytes.HasValue && serverConfig.MaxDocumentUploadBytes.Value > 0
                ? (int?)(serverConfig.MaxDocumentUploadBytes.Value / (1024 * 1024))
                : null,
            EnableReminderEmails = serverConfig.EnableReminderEmails,
            ReminderEmailDaysAhead = serverConfig.ReminderEmailDaysAhead,
            DefaultReminderEmail = serverConfig.DefaultReminderEmail ?? string.Empty,
            CustomLogoUrl = serverConfig.CustomLogoUrl ?? string.Empty,
            WebHookUrl = serverConfig.WebHookUrl ?? string.Empty,
            AllowedFileExtensions = serverConfig.AllowedFileExtensions != null
                ? string.Join(", ", serverConfig.AllowedFileExtensions)
                : string.Empty,
            InvariantApiEnabled = serverConfig.InvariantApiEnabled,
            CustomWidgetsEnabled = serverConfig.CustomWidgetsEnabled,
            Domain = serverConfig.Domain ?? string.Empty,
            MailHost = serverConfig.MailConfig?.Host ?? string.Empty,
            MailPort = serverConfig.MailConfig?.Port,
            MailUserName = serverConfig.MailConfig?.UserName ?? string.Empty,
            MailPassword = serverConfig.MailConfig?.Password ?? string.Empty,
            MailUseSsl = serverConfig.MailConfig?.UseSsl ?? false,
            MailFromEmail = serverConfig.MailConfig?.FromEmail ?? string.Empty,
            MailFromName = serverConfig.MailConfig?.FromName ?? string.Empty,
            UrgencyDaysUntilUrgent = serverConfig.ReminderUrgencyConfig?.DaysUntilUrgent,
            UrgencyDaysUntilVeryUrgent = serverConfig.ReminderUrgencyConfig?.DaysUntilVeryUrgent,
            UrgencyDaysUntilPastDue = serverConfig.ReminderUrgencyConfig?.DaysUntilPastDue
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
        serverConfig.OpenRegistration = model.OpenRegistration;
        serverConfig.DisableRegistration = model.DisableRegistration;
        serverConfig.EnableRootUserOidc = model.EnableRootUserOidc;
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
        serverConfig.DefaultReminderEmail = string.IsNullOrWhiteSpace(model.DefaultReminderEmail)
            ? null
            : model.DefaultReminderEmail.Trim();
        serverConfig.CustomLogoUrl = string.IsNullOrWhiteSpace(model.CustomLogoUrl)
            ? null
            : model.CustomLogoUrl.Trim();
        serverConfig.WebHookUrl = string.IsNullOrWhiteSpace(model.WebHookUrl)
            ? null
            : model.WebHookUrl.Trim();
        serverConfig.AllowedFileExtensions = string.IsNullOrWhiteSpace(model.AllowedFileExtensions)
            ? new List<string>()
            : model.AllowedFileExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        serverConfig.InvariantApiEnabled = model.InvariantApiEnabled;
        serverConfig.CustomWidgetsEnabled = model.CustomWidgetsEnabled;
        serverConfig.Domain = string.IsNullOrWhiteSpace(model.Domain) ? null : model.Domain.Trim();

        serverConfig.MailConfig ??= new MailConfig();
        serverConfig.MailConfig.Host = string.IsNullOrWhiteSpace(model.MailHost) ? null : model.MailHost.Trim();
        serverConfig.MailConfig.Port = model.MailPort ?? 0;
        serverConfig.MailConfig.UserName = string.IsNullOrWhiteSpace(model.MailUserName) ? null : model.MailUserName.Trim();
        serverConfig.MailConfig.Password = string.IsNullOrWhiteSpace(model.MailPassword) ? null : model.MailPassword;
        serverConfig.MailConfig.UseSsl = model.MailUseSsl;
        serverConfig.MailConfig.FromEmail = string.IsNullOrWhiteSpace(model.MailFromEmail) ? null : model.MailFromEmail.Trim();
        serverConfig.MailConfig.FromName = string.IsNullOrWhiteSpace(model.MailFromName) ? null : model.MailFromName.Trim();

        serverConfig.ReminderUrgencyConfig ??= new ReminderUrgencyConfig();
        if (model.UrgencyDaysUntilUrgent.HasValue)
        {
            serverConfig.ReminderUrgencyConfig.DaysUntilUrgent = model.UrgencyDaysUntilUrgent.Value;
        }
        if (model.UrgencyDaysUntilVeryUrgent.HasValue)
        {
            serverConfig.ReminderUrgencyConfig.DaysUntilVeryUrgent = model.UrgencyDaysUntilVeryUrgent.Value;
        }
        if (model.UrgencyDaysUntilPastDue.HasValue)
        {
            serverConfig.ReminderUrgencyConfig.DaysUntilPastDue = model.UrgencyDaysUntilPastDue.Value;
        }

        _configHelper.SaveServerConfig(serverConfig);

        _logger.LogInformation("Server settings updated by root user {UserId}.", userId);

        TempData["StatusMessage"] = "Server settings have been saved successfully.";
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
