using System.Security.Claims;
using CarCareTracker.Helper;
using CarCareTracker.Logic;
using CarCareTracker.Models.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class APIController : ControllerBase
{
    private readonly ILogger<APIController> _logger;
    private readonly VehicleLogic _vehicleLogic;
    private readonly UserLogic _userLogic;

    public APIController(
        ILogger<APIController> logger,
        VehicleLogic vehicleLogic,
        UserLogic userLogic)
    {
        _logger = logger;
        _vehicleLogic = vehicleLogic;
        _userLogic = userLogic;
    }

    /// <summary>
    /// Returns information about the currently authenticated user.
    /// </summary>
    [HttpGet("whoami")]
    public ActionResult<WhoAmIResponse> WhoAmI()
    {
        var name = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        var isAdminClaim = User.FindFirst("IsAdmin")?.Value;
        var isRootClaim = User.FindFirst("IsRootUser")?.Value;

        bool isAdmin = bool.TryParse(isAdminClaim, out var adminFlag) && adminFlag;
        bool isRootUser = bool.TryParse(isRootClaim, out var rootFlag) && rootFlag;

        var response = new WhoAmIResponse
        {
            UserName = name,
            Email = email,
            IsAdmin = isAdmin,
            IsRootUser = isRootUser
        };

        return Ok(response);
    }

    /// <summary>
    /// Returns version information for the running application.
    /// </summary>
    [HttpGet("version")]
    public ActionResult<ReleaseVersion> Version()
    {
        var current = StaticHelper.VersionNumber;

        var response = new ReleaseVersion
        {
            CurrentVersion = current,
            LatestVersion = null,
            HasUpdate = false
        };

        // TODO: In a later phase, fetch the latest release from GitHub and populate LatestVersion/HasUpdate.

        return Ok(response);
    }

    /// <summary>
    /// Returns dashboard-style information for all vehicles accessible to the current user.
    /// </summary>
    [HttpGet("vehicles")]
    public async Task<ActionResult<List<VehicleInfo>>> GetVehicles()
    {
        if (!TryGetUserContext(out var userId, out var isRootUser))
        {
            return Unauthorized();
        }

        List<int>? allowedVehicleIds = null;

        if (!isRootUser)
        {
            allowedVehicleIds = await _userLogic.GetAccessibleVehicleIdsForUserAsync(userId, isRootUser);
            if (allowedVehicleIds.Count == 0)
            {
                return Ok(new List<VehicleInfo>());
            }
        }

        var dashboard = await _vehicleLogic.GetVehicleDashboardAsync(
            userId,
            isRootUser,
            allowedVehicleIds?.Count > 0 ? allowedVehicleIds : null);

        var result = dashboard
            .Select(VehicleInfo.FromViewModel)
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Returns dashboard-style information for a single vehicle.
    /// </summary>
    [HttpGet("vehicle/info")]
    public async Task<ActionResult<VehicleInfo>> GetVehicleInfo([FromQuery] int vehicleId)
    {
        if (!TryGetUserContext(out var userId, out var isRootUser))
        {
            return Unauthorized();
        }

        if (!isRootUser)
        {
            var hasAccess = await _userLogic.UserHasAccessToVehicleAsync(userId, isRootUser, vehicleId);
            if (!hasAccess)
            {
                return Forbid();
            }
        }

        var vm = await _vehicleLogic.GetVehicleDashboardEntryAsync(vehicleId);
        if (vm == null)
        {
            return NotFound();
        }

        var info = VehicleInfo.FromViewModel(vm);
        return Ok(info);
    }

    private bool TryGetUserContext(out int userId, out bool isRootUser)
    {
        userId = 0;
        isRootUser = false;

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out userId))
        {
            _logger.LogWarning("Could not parse user id from NameIdentifier claim.");
            return false;
        }

        var isRootClaim = User.FindFirst("IsRootUser")?.Value;
        isRootUser = bool.TryParse(isRootClaim, out var rootFlag) && rootFlag;

        return true;
    }
}
