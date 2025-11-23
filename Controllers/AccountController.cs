using System.Security.Claims;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly IUserRecordDataAccess _userRecordDataAccess;
    private readonly IPasswordHelper _passwordHelper;

    public AccountController(
        ILogger<AccountController> logger,
        IUserRecordDataAccess userRecordDataAccess,
        IPasswordHelper passwordHelper)
    {
        _logger = logger;
        _userRecordDataAccess = userRecordDataAccess;
        _passwordHelper = passwordHelper;
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (userId, _) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var user = await _userRecordDataAccess.GetUserByIdAsync(userId.Value);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Unable to find your user record.");
            return View(model);
        }

        if (!_passwordHelper.VerifyPassword(model.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(nameof(ChangePasswordModel.CurrentPassword), "The current password is incorrect.");
            return View(model);
        }

        user.PasswordHash = _passwordHelper.HashPassword(model.NewPassword);

        await _userRecordDataAccess.SaveUserAsync(user);

        _logger.LogInformation("User {UserName} changed their password.", user.UserName ?? user.Id.ToString());

        TempData["StatusMessage"] = "Your password has been updated.";

        return RedirectToAction(nameof(ChangePassword));
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
