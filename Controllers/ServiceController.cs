using System.Security.Claims;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Logic;
using CarCareTracker.Models.ServiceRecord;
using CarCareTracker.Models.Vehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class ServiceController : Controller
{
    private readonly ILogger<ServiceController> _logger;
    private readonly IServiceRecordDataAccess _serviceDataAccess;
    private readonly IVehicleDataAccess _vehicleDataAccess;
    private readonly UserLogic _userLogic;

    public ServiceController(
        ILogger<ServiceController> logger,
        IServiceRecordDataAccess serviceDataAccess,
        IVehicleDataAccess vehicleDataAccess,
        UserLogic userLogic)
    {
        _logger = logger;
        _serviceDataAccess = serviceDataAccess;
        _vehicleDataAccess = vehicleDataAccess;
        _userLogic = userLogic;
    }

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

        var records = await _serviceDataAccess.GetServiceRecordsForVehicleAsync(vehicleId, null);
        ViewBag.Vehicle = vehicle;
        return View(records);
    }

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

        var model = new ServiceRecord
        {
            VehicleId = vehicleId,
            Date = DateTime.UtcNow.Date
        };

        ViewBag.Vehicle = vehicle;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceRecord model)
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

        await _serviceDataAccess.SaveServiceRecordAsync(model);
        _logger.LogInformation("Service record saved for vehicle {VehicleId}.", model.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = model.VehicleId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await _serviceDataAccess.GetServiceRecordAsync(id);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceRecord model)
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

        await _serviceDataAccess.SaveServiceRecordAsync(model);
        _logger.LogInformation("Service record {RecordId} updated for vehicle {VehicleId}.", model.Id, model.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = model.VehicleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _serviceDataAccess.GetServiceRecordAsync(id);
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

        await _serviceDataAccess.DeleteServiceRecordAsync(id);
        _logger.LogInformation("Service record {RecordId} deleted for vehicle {VehicleId}.", id, record.VehicleId);

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

