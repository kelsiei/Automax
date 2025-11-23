using System.Security.Claims;
using System.Text;
using CarCareTracker.Helper;
using CarCareTracker.Logic;
using CarCareTracker.Models.Reminder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class ReminderEmailController : Controller
{
    private readonly ILogger<ReminderEmailController> _logger;
    private readonly ReminderEmailLogic _reminderEmailLogic;
    private readonly ConfigHelper _configHelper;
    private readonly MailHelper _mailHelper;

    public ReminderEmailController(
        ILogger<ReminderEmailController> logger,
        ReminderEmailLogic reminderEmailLogic,
        ConfigHelper configHelper,
        MailHelper mailHelper)
    {
        _logger = logger;
        _reminderEmailLogic = reminderEmailLogic;
        _configHelper = configHelper;
        _mailHelper = mailHelper;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (_, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        var serverConfig = _configHelper.LoadServerConfig();
        var digests = await _reminderEmailLogic.BuildReminderEmailDigestsAsync();

        ViewBag.EnableReminderEmails = serverConfig.EnableReminderEmails;
        ViewBag.ReminderEmailDaysAhead = serverConfig.ReminderEmailDaysAhead;

        return View(digests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send()
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        var serverConfig = _configHelper.LoadServerConfig();

        if (!serverConfig.EnableReminderEmails)
        {
            TempData["StatusMessage"] = "Reminder email digests are currently disabled in settings.";
            return RedirectToAction(nameof(Index));
        }

        var digests = await _reminderEmailLogic.BuildReminderEmailDigestsAsync();

        if (!digests.Any())
        {
            TempData["StatusMessage"] = "No reminder email digests need to be sent at this time.";
            return RedirectToAction(nameof(Index));
        }

        var sentCount = 0;

        foreach (var digest in digests)
        {
            try
            {
                var subject = "CarCareTracker – Upcoming vehicle reminders";

                var bodyBuilder = new StringBuilder();

                bodyBuilder.AppendLine($"Hello {digest.UserName},");
                bodyBuilder.AppendLine();
                bodyBuilder.AppendLine("Here is your upcoming reminder digest:");
                bodyBuilder.AppendLine();

                foreach (var r in digest.Reminders)
                {
                    var dueDateText = r.DueDate?.ToShortDateString() ?? "(no date)";
                    var vehicle = $"{r.Year} {r.Make} {r.Model} ({r.LicensePlate})";
                    var urgency = r.Urgency.ToString();
                    var tags = string.IsNullOrWhiteSpace(r.Tags) ? string.Empty : $" | Tags: {r.Tags}";
                    var odo = r.TargetOdometer.HasValue ? $" | Target odometer: {r.TargetOdometer.Value}" : string.Empty;

                    bodyBuilder.AppendLine(
                        $"{dueDateText} – {vehicle}: {r.Description} (Urgency: {urgency}{tags}{odo})");
                }

                bodyBuilder.AppendLine();
                bodyBuilder.AppendLine("This is an automated message from CarCareTracker.");

                var body = bodyBuilder.ToString();

                await _mailHelper.SendReminderDigestEmailAsync(
                    digest.EmailAddress,
                    subject,
                    body);

                sentCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send reminder digest email to {EmailAddress}.",
                    digest.EmailAddress);
            }
        }

        TempData["StatusMessage"] = sentCount == 0
            ? "No reminder email digests were successfully sent."
            : $"Reminder email digests sent to {sentCount} user(s).";

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

