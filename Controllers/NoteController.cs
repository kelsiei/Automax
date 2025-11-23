using System.Security.Claims;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Logic;
using CarCareTracker.Models.Note;
using CarCareTracker.Models.Vehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class NoteController : Controller
{
    private readonly ILogger<NoteController> _logger;
    private readonly INoteDataAccess _noteDataAccess;
    private readonly IVehicleDataAccess _vehicleDataAccess;
    private readonly UserLogic _userLogic;

    public NoteController(
        ILogger<NoteController> logger,
        INoteDataAccess noteDataAccess,
        IVehicleDataAccess vehicleDataAccess,
        UserLogic userLogic)
    {
        _logger = logger;
        _noteDataAccess = noteDataAccess;
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

        var notes = await _noteDataAccess.GetNotesForVehicleAsync(vehicleId, null);
        ViewBag.Vehicle = vehicle;
        return View(notes);
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

        var model = new Note
        {
            VehicleId = vehicleId,
            CreatedAt = DateTime.UtcNow
        };

        ViewBag.Vehicle = vehicle;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Note model)
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

        await _noteDataAccess.SaveNoteAsync(model);
        _logger.LogInformation("Note saved for vehicle {VehicleId}.", model.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = model.VehicleId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var note = await _noteDataAccess.GetNoteAsync(id);
        if (note == null)
        {
            return NotFound();
        }

        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, note.VehicleId))
        {
            return Forbid();
        }

        var vehicle = await _vehicleDataAccess.GetVehicleAsync(note.VehicleId);
        ViewBag.Vehicle = vehicle;

        return View(note);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Note model)
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

        await _noteDataAccess.SaveNoteAsync(model);
        _logger.LogInformation("Note {NoteId} updated for vehicle {VehicleId}.", model.Id, model.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = model.VehicleId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var note = await _noteDataAccess.GetNoteAsync(id);
        if (note == null)
        {
            return NotFound();
        }

        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, note.VehicleId))
        {
            return Forbid();
        }

        await _noteDataAccess.DeleteNoteAsync(id);
        _logger.LogInformation("Note {NoteId} deleted for vehicle {VehicleId}.", id, note.VehicleId);

        return RedirectToAction(nameof(Index), new { vehicleId = note.VehicleId });
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

