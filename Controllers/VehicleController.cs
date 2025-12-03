using System.Security.Claims;
using Automax.External.Interfaces;
using Automax.Logic;
using Automax.Models.Vehicle;
using Automax.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automax.Controllers;

[Authorize]
public class VehicleController : Controller
{
    private readonly ILogger<VehicleController> _logger;
    private readonly IVehicleDataAccess _vehicleDataAccess;
    private readonly VehicleLogic _vehicleLogic;
    private readonly UserLogic _userLogic;
    private readonly IUserAccessDataAccess _userAccessDataAccess;

    public VehicleController(
        ILogger<VehicleController> logger,
        IVehicleDataAccess vehicleDataAccess,
        VehicleLogic vehicleLogic,
        UserLogic userLogic,
        IUserAccessDataAccess userAccessDataAccess)
    {
        _logger = logger;
        _vehicleDataAccess = vehicleDataAccess;
        _vehicleLogic = vehicleLogic;
        _userLogic = userLogic;
        _userAccessDataAccess = userAccessDataAccess;
    }

    // GET: /Vehicle
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 25, string? searchTerm = null, bool showOnlyUrgent = false)
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        List<int>? allowedIds = null;
        if (!isRootUser)
        {
            allowedIds = await _userLogic.GetAccessibleVehicleIdsForUserAsync(userId.Value, isRootUser);
        }

        var models = await _vehicleLogic.GetVehicleDashboardAsync(
            userId.Value,
            isRootUser,
            isRootUser ? null : allowedIds,
            searchTerm);

        if (showOnlyUrgent)
        {
            models = models.Where(v => v.HasUrgentReminders).ToList();
        }

        var totalItems = models.Count;
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
        if (page < 1)
        {
            page = 1;
        }
        if (page > totalPages)
        {
            page = totalPages;
        }

        var pagedVehicles = models
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var viewModel = new VehicleIndexViewModel
        {
            Vehicles = pagedVehicles,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            SearchTerm = searchTerm ?? string.Empty,
            ShowOnlyUrgent = showOnlyUrgent
        };

        return View(viewModel);
    }

    // GET: /Vehicle/Create
    [HttpGet]
    public IActionResult Create()
    {
        var model = new Vehicle
        {
            Year = DateTime.UtcNow.Year
        };
        return View(model);
    }

    // POST: /Vehicle/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Vehicle vehicleModel)
    {
        if (!ModelState.IsValid)
        {
            return View(vehicleModel);
        }

        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var newId = await _vehicleDataAccess.SaveVehicleAsync(vehicleModel);
        vehicleModel.Id = newId;

        if (!isRootUser)
        {
            var access = new UserAccess
            {
                UserId = userId.Value,
                VehicleId = vehicleModel.Id,
                CanEdit = true
            };
            await _userAccessDataAccess.SaveUserAccessAsync(access);
        }

        _logger.LogInformation("Vehicle {Year} {Make} {Model} created.", vehicleModel.Year, vehicleModel.Make, vehicleModel.Model);

        return RedirectToAction(nameof(Index));
    }

    // GET: /Vehicle/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, id))
        {
            return Forbid();
        }

        var vehicle = await _vehicleDataAccess.GetVehicleAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        return View(vehicle);
    }

    // POST: /Vehicle/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Vehicle vehicleModel)
    {
        if (id != vehicleModel.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(vehicleModel);
        }

        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, id))
        {
            return Forbid();
        }

        await _vehicleDataAccess.SaveVehicleAsync(vehicleModel);
        _logger.LogInformation("Vehicle {VehicleId} updated.", vehicleModel.Id);

        return RedirectToAction(nameof(Index));
    }

    // POST: /Vehicle/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!await _userLogic.UserHasAccessToVehicleAsync(userId.Value, isRootUser, id))
        {
            return Forbid();
        }

        await _vehicleDataAccess.DeleteVehicleAsync(id);
        _logger.LogInformation("Vehicle {VehicleId} deleted.", id);

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
