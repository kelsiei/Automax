using System.Security.Claims;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Models.Login;
using CarCareTracker.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[AllowAnonymous]
public class LoginController : Controller
{
    private readonly ILogger<LoginController> _logger;
    private readonly IUserRecordDataAccess _userRecordDataAccess;
    private const string AuthCookieName = "CarCareTrackerAuth";

    public LoginController(
        ILogger<LoginController> logger,
        IUserRecordDataAccess userRecordDataAccess,
        IPasswordHelper passwordHelper)
    {
        _logger = logger;
        _userRecordDataAccess = userRecordDataAccess;
        _passwordHelper = passwordHelper;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        await EnsureRootUserExistsAsync();
        return View(new LoginModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(LoginModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userRecordDataAccess.GetUserByUserNameAsync(model.UserName);
        if (user == null || !_passwordHelper.VerifyPassword(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var userId = user.Id.ToString();
        var userName = user.UserName ?? string.Empty;
        var email = user.EmailAddress ?? string.Empty;
        var isAdmin = user.IsAdmin ? "true" : "false";
        var isRoot = user.IsRootUser ? "true" : "false";

        var cookieValue = string.Join("|", userId, userName, email, isAdmin, isRoot);

        var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            Expires = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : (DateTimeOffset?)null
        };

        Response.Cookies.Append(AuthCookieName, cookieValue, cookieOptions);

        _logger.LogInformation("User {UserName} signed in.", userName);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookieName);
        _logger.LogInformation("User logged out.");
        return RedirectToAction("LoggedOut");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult LoggedOut()
    {
        return View();
    }

    private async Task EnsureRootUserExistsAsync()
    {
        if (await _userRecordDataAccess.AnyUsersAsync())
        {
            return;
        }

        var defaultUserName = "root";
        var defaultPassword = "password";
        var defaultEmail = "root@example.com";

        var user = new UserData
        {
            UserName = defaultUserName,
            EmailAddress = defaultEmail,
            IsAdmin = true,
            IsRootUser = true,
            PasswordHash = _passwordHelper.HashPassword(defaultPassword)
        };

        await _userRecordDataAccess.SaveUserAsync(user);

        _logger.LogInformation("Seeded default root user with username '{UserName}'.", defaultUserName);
    }

    private readonly IPasswordHelper _passwordHelper;
}
