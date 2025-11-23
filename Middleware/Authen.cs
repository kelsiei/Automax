using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarCareTracker.Middleware;

public class Authen : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string AuthCookieName = "CarCareTrackerAuth";

    public Authen(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(AuthCookieName, out var cookieValue) ||
            string.IsNullOrWhiteSpace(cookieValue))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        try
        {
            // Very simple cookie format: "userId|userName|email|isAdmin|isRoot"
            var parts = cookieValue.Split('|');

            if (parts.Length < 5)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid auth cookie format."));
            }

            var userId = parts[0];
            var userName = parts[1];
            var email = parts[2];
            var isAdmin = parts[3];
            var isRoot = parts[4];

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Email, email),
                new Claim("IsAdmin", isAdmin),
                new Claim("IsRootUser", isRoot)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to authenticate using cookie.");
            return Task.FromResult(AuthenticateResult.Fail("Failed to authenticate."));
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Login");
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Error/Status/403");
        return Task.CompletedTask;
    }

    // TODO: Align with full UserData/roles model and persistent storage in later phases.
}
