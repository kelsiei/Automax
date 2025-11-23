using System.Security.Claims;
using CarCareTracker.Logic;
using CarCareTracker.Models.Home;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HomeDashboardLogic _dashboardLogic;

    public HomeController(
        ILogger<HomeController> logger,
        HomeDashboardLogic dashboardLogic)
    {
        _logger = logger;
        _dashboardLogic = dashboardLogic;
    }

    // GET: /
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        var model = await _dashboardLogic.BuildDashboardAsync(userId, isRootUser);

        var isAuthenticated = User?.Identity?.IsAuthenticated ?? false;
        model.IsAuthenticated = isAuthenticated;

        if (isAuthenticated)
        {
            model.UserName = User?.Identity?.Name ?? string.Empty;
            model.IsAdmin = User.HasClaim("IsAdmin", "true");
            model.IsRootUser = isRootUser;
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
