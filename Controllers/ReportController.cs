using System.Security.Claims;
using System.Text;
using CarCareTracker.Logic;
using CarCareTracker.Models.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarCareTracker.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly ILogger<ReportController> _logger;
    private readonly ReportLogic _reportLogic;

    public ReportController(
        ILogger<ReportController> logger,
        ReportLogic reportLogic)
    {
        _logger = logger;
        _reportLogic = reportLogic;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 25, string? searchTerm = null, bool showOnlyUrgent = false)
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var summaries = await _reportLogic.GetVehicleReportSummariesAsync(userId.Value, isRootUser, searchTerm);
        if (showOnlyUrgent)
        {
            summaries = summaries.Where(s => s.HasUrgentReminders).ToList();
        }
        var totalItems = summaries.Count;
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
        if (page < 1)
        {
            page = 1;
        }
        if (page > totalPages)
        {
            page = totalPages;
        }

        var pagedSummaries = summaries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var viewModel = new ReportIndexViewModel
        {
            VehicleSummaries = pagedSummaries,
            CurrentPage = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages,
            SearchTerm = searchTerm ?? string.Empty,
            ShowOnlyUrgent = showOnlyUrgent
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv()
    {
        var (userId, isRootUser) = GetCurrentUserContext();
        if (userId == null)
        {
            return RedirectToAction("Index", "Login");
        }

        var csv = await _reportLogic.GetVehicleReportCsvAsync(userId.Value, isRootUser);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileName = $"vehicle-report-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        return File(bytes, "text/csv", fileName);
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
