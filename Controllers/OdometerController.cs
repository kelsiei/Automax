using System.Security.Claims;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Logic;
using CarCareTracker.Models.OdometerRecord;
using CarCareTracker.Models.Vehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class OdometerController : Controller
{
    private readonly ILogger<OdometerController> _logger;
    private readonly IOdometerRecordDataAccess _odometerDataAccess;
    private readonly IVehicleDataAccess _vehicleDataAccess;
    private readonly UserLogic _userLogic;

    public OdometerController(
        ILogger<OdometerController> logger,
        IOdometerRecordDataAccess odometerDataAccess,
        IVehicleDataAccess vehicleDataAccess,
        UserLogic userLogic)
    {
        _logger = logger;
        _odometerDataAccess = odometerDataAccess;
        _vehicleDataAccess = vehicleDataAccess;
        _userLogic = userLogic;
    }

    // GET: /Odometer?vehicleId=123
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

        var records = await _odometerDataAccess.GetOdometerRecordsForVehicleAsync(vehicleId, null);

        ViewBag.Vehicle = vehicle;
        return View(records);
    }

    // GET: /Odometer/Create?vehicleId=123
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

        var model = new OdometerRecord
        {
            VehicleId = vehicleId,
            Date = DateTime.UtcNow.Date
        };

        ViewBag.Vehicle = vehicle;
        return View(model);
    }

    // POST: /Odometer/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OdometerRecord model)
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

        await _odometerDataAccess.SaveOdometerRecordAsync(model);
        _logger.LogInformation("Odometer record saved for vehicle {VehicleId} at {Odometer}.", model.VehicleId, model.Odometer);

        return RedirectToAction(nameof(Index), new { vehicleId = model.VehicleId });
    }

    // GET: /Odometer/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await _odometerDataAccess.GetOdometerRecordAsync(id);
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

    // POST: /Odometer/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OdometerRecord model)
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

        await _odometerDataAccess.SaveOdometerRecordAsync(model);
        _logger.LogInformation("Odometer record {RecordId} updated for vehicle {VehicleId}.", model.Id, model.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = model.VehicleId });
    }

    // POST: /Odometer/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _odometerDataAccess.GetOdometerRecordAsync(id);
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

        await _odometerDataAccess.DeleteOdometerRecordAsync(id);
        _logger.LogInformation("Odometer record {RecordId} deleted for vehicle {VehicleId}.", id, record.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = record.VehicleId });
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

