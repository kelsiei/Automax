using System.Security.Claims;
using Automax.External.Interfaces;
using Automax.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Automax.Controllers;

[Authorize]
public class UserSettingsController : Controller
{
    private readonly IUserConfigDataAccess _userConfigDataAccess;
    private readonly ILogger<UserSettingsController> _logger;

    public UserSettingsController(
        IUserConfigDataAccess userConfigDataAccess,
        ILogger<UserSettingsController> logger)
    {
        _userConfigDataAccess = userConfigDataAccess;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var config = await _userConfigDataAccess.GetUserConfigAsync(userId.Value);
        var prefs = config?.Config ?? new UserConfig();

        var vm = new UserPreferencesViewModel
        {
            PreferredUnits = prefs.PreferredUnits ?? string.Empty,
            DefaultLandingPage = string.IsNullOrWhiteSpace(prefs.DefaultLandingPage) ? "Dashboard" : prefs.DefaultLandingPage,
            ShowFuelWidget = prefs.ShowFuelWidget
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(UserPreferencesViewModel model)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var current = await _userConfigDataAccess.GetUserConfigAsync(userId.Value);
        var cfg = current?.Config ?? new UserConfig();

        cfg.PreferredUnits = string.IsNullOrWhiteSpace(model.PreferredUnits) ? null : model.PreferredUnits.Trim();
        cfg.DefaultLandingPage = string.IsNullOrWhiteSpace(model.DefaultLandingPage) ? "Dashboard" : model.DefaultLandingPage.Trim();
        cfg.ShowFuelWidget = model.ShowFuelWidget;

        await _userConfigDataAccess.SaveUserConfigAsync(userId.Value, cfg);

        _logger.LogInformation("User {UserId} updated preferences.", userId);
        TempData["StatusMessage"] = "Your preferences have been saved.";
        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (idClaim != null && int.TryParse(idClaim.Value, out var parsed))
        {
            return parsed;
        }
        return null;
    }
}
