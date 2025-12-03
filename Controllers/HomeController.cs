using System.Security.Claims;
using Automax.Logic;
using Automax.Models.Home;
using Automax.External.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Automax.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HomeDashboardLogic _dashboardLogic;
    private readonly IUserConfigDataAccess _userConfigDataAccess;

    public HomeController(
        ILogger<HomeController> logger,
        HomeDashboardLogic dashboardLogic,
        IUserConfigDataAccess userConfigDataAccess)
    {
        _logger = logger;
        _dashboardLogic = dashboardLogic;
        _userConfigDataAccess = userConfigDataAccess;
    }

    // GET: /
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (userId, isRootUser) = GetCurrentUserContext();

        if (userId.HasValue)
        {
            var userConfig = await _userConfigDataAccess.GetUserConfigAsync(userId.Value);
            var defaultLanding = userConfig?.Config?.DefaultLandingPage?.Trim() ?? string.Empty;
            if (string.Equals(defaultLanding, "Garage", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Vehicle");
            }
        }

        var model = await _dashboardLogic.BuildDashboardAsync(userId, isRootUser);

        var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
        model.IsAuthenticated = isAuthenticated;

        if (isAuthenticated && User != null)
        {
            model.UserName = User.Identity?.Name ?? string.Empty;
            model.IsAdmin = User.HasClaim("IsAdmin", "true");
            model.IsRootUser = isRootUser;
        }

        if (userId.HasValue)
        {
            var userConfig = await _userConfigDataAccess.GetUserConfigAsync(userId.Value);
            if (userConfig?.Config != null)
            {
                model.ShowFuelWidget = userConfig.Config.ShowFuelWidget;
                model.PreferredUnits = userConfig.Config.PreferredUnits ?? string.Empty;
            }
        }

        return View(model);
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
