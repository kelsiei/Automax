using CarCareTracker.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CarCareTracker.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task AddsExpectedHeaders()
    {
        var context = new DefaultHttpContext();
        var logger = Mock.Of<ILogger<SecurityHeadersMiddleware>>();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, logger);

        await middleware.Invoke(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("SAMEORIGIN", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal("0", context.Response.Headers["X-XSS-Protection"]);
        Assert.True(context.Response.Headers.TryGetValue("Content-Security-Policy", out var csp));
        Assert.Contains("default-src 'self'", csp.ToString());
    }

    [Fact]
    public async Task OverwritesExistingHeaders()
    {
        var context = new DefaultHttpContext();
        context.Response.Headers["X-Frame-Options"] = "ALLOWALL";

        var logger = Mock.Of<ILogger<SecurityHeadersMiddleware>>();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, logger);

        await middleware.Invoke(context);

        Assert.Equal("SAMEORIGIN", context.Response.Headers["X-Frame-Options"]);
    }
}

