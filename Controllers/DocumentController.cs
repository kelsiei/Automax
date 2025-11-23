using System.Security.Claims;
using CarCareTracker.External.Interfaces;
using CarCareTracker.Helper;
using CarCareTracker.Logic;
using CarCareTracker.Models.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CarCareTracker.Controllers;

[Authorize]
public class DocumentController : Controller
{
    private readonly ILogger<DocumentController> _logger;
    private readonly FileHelper _fileHelper;
    private readonly IVehicleDataAccess _vehicleDataAccess;
    private readonly UserLogic _userLogic;
    private readonly ServerConfig _serverConfig;

    public DocumentController(
        ILogger<DocumentController> logger,
        FileHelper fileHelper,
        IVehicleDataAccess vehicleDataAccess,
        UserLogic userLogic,
        IOptions<ServerConfig> serverConfigOptions)
    {
        _logger = logger;
        _fileHelper = fileHelper;
        _vehicleDataAccess = vehicleDataAccess;
        _userLogic = userLogic;
        _serverConfig = serverConfigOptions.Value;
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

        var documents = await _fileHelper.GetVehicleDocumentsAsync(vehicleId);

        ViewBag.Vehicle = vehicle;
        return View(documents);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int vehicleId, IFormFile file)
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

        if (file == null || file.Length == 0)
        {
            TempData["DocumentStatus"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Index), new { vehicleId });
        }

        var configuredMax = _serverConfig.MaxDocumentUploadBytes;
        var effectiveMaxBytes = (configuredMax.HasValue && configuredMax.Value > 0)
            ? configuredMax.Value
            : 10L * 1024L * 1024L;
        var effectiveMaxMb = Math.Max(1, (int)Math.Ceiling(effectiveMaxBytes / (1024.0 * 1024.0)));

        if (file.Length > effectiveMaxBytes)
        {
            TempData["DocumentStatus"] = $"The selected file is too large. The maximum allowed size is {effectiveMaxMb} MB.";
            return RedirectToAction(nameof(Index), new { vehicleId });
        }

        var allowedExtensions = _serverConfig.AllowedFileExtensions ?? new List<string>();
        var savedName = await _fileHelper.SaveVehicleDocumentAsync(vehicleId, file, allowedExtensions);

        if (savedName == null)
        {
            TempData["DocumentStatus"] = "The selected file type is not allowed.";
        }
        else
        {
            TempData["DocumentStatus"] = "File uploaded successfully.";
        }

        return RedirectToAction(nameof(Index), new { vehicleId });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int vehicleId, string fileName)
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

        var stream = _fileHelper.OpenVehicleDocumentStream(vehicleId, fileName);
        if (stream == null)
        {
            return NotFound();
        }

        const string contentType = "application/octet-stream";
        return File(stream, contentType, fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int vehicleId, string fileName)
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

        var deleted = await _fileHelper.DeleteVehicleDocumentAsync(vehicleId, fileName);
        TempData["DocumentStatus"] = deleted
            ? "File deleted."
            : "File could not be deleted.";

        return RedirectToAction(nameof(Index), new { vehicleId });
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
