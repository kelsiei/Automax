using System.Security.Claims;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.Admin;
using CarCareTracker.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly ILogger<AdminController> _logger;
    private readonly IUserRecordDataAccess _userRecordDataAccess;
    private readonly IPasswordHelper _passwordHelper;

    public AdminController(
        ILogger<AdminController> logger,
        IUserRecordDataAccess userRecordDataAccess,
        IPasswordHelper passwordHelper)
    {
        _logger = logger;
        _userRecordDataAccess = userRecordDataAccess;
        _passwordHelper = passwordHelper;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var (_, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        var users = await _userRecordDataAccess.GetAllUsersAsync();
        users = users
            .OrderByDescending(u => u.IsRootUser)
            .ThenBy(u => u.UserName)
            .ToList();

        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var (_, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        return View(new AdminUserCreateModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUserCreateModel model)
    {
        var (_, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _userRecordDataAccess.GetUserByUserNameAsync(model.UserName);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.UserName), "A user with this username already exists.");
            return View(model);
        }

        var user = new UserData
        {
            UserName = model.UserName.Trim(),
            EmailAddress = model.EmailAddress.Trim(),
            IsAdmin = model.IsAdmin,
            IsRootUser = false,
            PasswordHash = _passwordHelper.HashPassword(model.Password)
        };

        await _userRecordDataAccess.SaveUserAsync(user);

        _logger.LogInformation("Admin created user '{UserName}' (Id {UserId}).", user.UserName, user.Id);

        TempData["StatusMessage"] = $"User '{user.UserName}' was created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var (_, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        var user = await _userRecordDataAccess.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        var model = new AdminUserEditModel
        {
            Id = user.Id,
            UserName = user.UserName,
            EmailAddress = user.EmailAddress,
            IsAdmin = user.IsAdmin,
            IsRootUser = user.IsRootUser
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminUserEditModel model)
    {
        var (_, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userRecordDataAccess.GetUserByIdAsync(model.Id);
        if (user == null)
        {
            return NotFound();
        }

        if (!user.IsRootUser)
        {
            user.UserName = model.UserName.Trim();
            user.IsAdmin = model.IsAdmin;
        }

        user.EmailAddress = model.EmailAddress.Trim();

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            user.PasswordHash = _passwordHelper.HashPassword(model.NewPassword);
        }

        await _userRecordDataAccess.SaveUserAsync(user);

        _logger.LogInformation("Admin updated user '{UserName}' (Id {UserId}).", user.UserName, user.Id);

        TempData["StatusMessage"] = $"User '{user.UserName}' was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (_, isRootUser) = GetCurrentUserContext();
        if (!isRootUser)
        {
            return Forbid();
        }

        var user = await _userRecordDataAccess.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.IsRootUser)
        {
            TempData["StatusMessage"] = "The root user cannot be deleted.";
            return RedirectToAction(nameof(Index));
        }

        await _userRecordDataAccess.DeleteUserAsync(id);

        _logger.LogInformation("Admin deleted user '{UserName}' (Id {UserId}).", user.UserName, user.Id);

        TempData["StatusMessage"] = $"User '{user.UserName}' was deleted.";
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
