using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CarCareTracker.Controllers;

public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [Route("Error")]
    public IActionResult Index()
    {
        return View();
    }

    [Route("Error/Status/{code}")]
    public IActionResult Status(int code)
    {
        _logger.LogWarning("HTTP error status {StatusCode} encountered.", code);
        ViewBag.StatusCode = code;

        switch (code)
        {
            case 401:
                ViewData["Title"] = "Unauthorized";
                return View("401");
            case 403:
                ViewData["Title"] = "Forbidden";
                return View("403");
            case 404:
                ViewData["Title"] = "Not Found";
                return View("404");
            default:
                ViewData["Title"] = "Error";
                return View("Index");
        }
    }

    // TODO: add dedicated views/logic for 401/403/404 in later phases.
}
