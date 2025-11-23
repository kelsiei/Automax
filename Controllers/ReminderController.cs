using System.Security.Claims;
using System.Text;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Logic;
using CarCareTracker.Models.Reminder;
using CarCareTracker.Models.Vehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class ReminderController : Controller
{
    private readonly ILogger<ReminderController> _logger;
    private readonly IReminderRecordDataAccess _reminderDataAccess;
    private readonly IVehicleDataAccess _vehicleDataAccess;
    private readonly UserLogic _userLogic;
    private readonly ReminderLogic _reminderLogic;
    private readonly ReminderHelper _reminderHelper;

    public ReminderController(
        ILogger<ReminderController> logger,
        IReminderRecordDataAccess reminderDataAccess,
        IVehicleDataAccess vehicleDataAccess,
        UserLogic userLogic,
        ReminderLogic reminderLogic,
        ReminderHelper reminderHelper)
    {
        _logger = logger;
        _reminderDataAccess = reminderDataAccess;
        _vehicleDataAccess = vehicleDataAccess;
        _userLogic = userLogic;
        _reminderLogic = reminderLogic;
        _reminderHelper = reminderHelper;
    }

    // GET: /Reminder?vehicleId=123
    [HttpGet]
    public async Task<IActionResult> Index(int vehicleId)
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, vehicleId))
        {
            return Forbid();
        }

        var vehicle = await _vehicleDataAccess.GetVehicleAsync(vehicleId);
        if (vehicle == null)
        {
            return NotFound();
        }

        var reminders = await _reminderDataAccess.GetReminderRecordsForVehicleAsync(vehicleId, null);

        ViewBag.Vehicle = vehicle;
        return View(reminders);
    }

    // GET: /Reminder/Create?vehicleId=123
    [HttpGet]
    public async Task<IActionResult> Create(int vehicleId)
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, vehicleId))
        {
            return Forbid();
        }

        var vehicle = await _vehicleDataAccess.GetVehicleAsync(vehicleId);
        if (vehicle == null)
        {
            return NotFound();
        }

        var model = new ReminderRecord
        {
            VehicleId = vehicleId,
            DueDate = DateTime.UtcNow.Date
        };

        ViewBag.Vehicle = vehicle;
        return View(model);
    }

    // POST: /Reminder/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReminderRecord model)
    {
        if (!ModelState.IsValid)
        {
            Vehicle? vehicleForView = null;
            if (model.VehicleId != 0)
            {
                vehicleForView = await _vehicleDataAccess.GetVehicleAsync(model.VehicleId);
            }

            ViewBag.Vehicle = vehicleForView;
            return View(model);
        }

        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, model.VehicleId))
        {
            return Forbid();
        }

        await _reminderDataAccess.SaveReminderRecordAsync(model);
        _logger.LogInformation("Reminder record saved for vehicle {VehicleId}.", model.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = model.VehicleId });
    }

    // GET: /Reminder/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await _reminderDataAccess.GetReminderRecordAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, record.VehicleId))
        {
            return Forbid();
        }

        var vehicle = await _vehicleDataAccess.GetVehicleAsync(record.VehicleId);
        ViewBag.Vehicle = vehicle;

        return View(record);
    }

    // POST: /Reminder/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReminderRecord model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var vehicleForView = await _vehicleDataAccess.GetVehicleAsync(model.VehicleId);
            ViewBag.Vehicle = vehicleForView;
            return View(model);
        }

        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, model.VehicleId))
        {
            return Forbid();
        }

        await _reminderDataAccess.SaveReminderRecordAsync(model);
        _logger.LogInformation("Reminder record {RecordId} updated for vehicle {VehicleId}.", model.Id, model.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = model.VehicleId });
    }

    // POST: /Reminder/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _reminderDataAccess.GetReminderRecordAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, record.VehicleId))
        {
            return Forbid();
        }

        await _reminderDataAccess.DeleteReminderRecordAsync(id);
        _logger.LogInformation("Reminder record {RecordId} deleted for vehicle {VehicleId}.", id, record.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = record.VehicleId });
    }

    [HttpGet]
    public async Task<IActionResult> Calendar()
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var reminders = await _reminderLogic.GetDateBasedRemindersForUserAsync(userId.Value, isRootUser);
        var ordered = reminders
            .OrderBy(r => r.DueDate ?? DateTime.MaxValue)
            .ThenBy(r => r.Year)
            .ThenBy(r => r.Make)
            .ThenBy(r => r.Model)
            .ThenBy(r => r.LicensePlate)
            .ThenBy(r => r.Description)
            .ToList();

        return View(ordered);
    }

    [HttpGet]
    public async Task<IActionResult> CalendarFeed()
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var reminders = await _reminderLogic.GetDateBasedRemindersForUserAsync(userId.Value, isRootUser);
        var calendarName = "CarCareTracker Reminders";
        var ics = _reminderHelper.BuildICalendarFeed(reminders, calendarName);
        var bytes = Encoding.UTF8.GetBytes(ics);
        var fileName = $"carcare-reminders-{DateTime.UtcNow:yyyyMMddHHmmss}.ics";

        return File(bytes, "text/calendar", fileName);
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
