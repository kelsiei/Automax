using System;
using Xunit;

namespace Automax.Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class PostgresIntegrationFactAttribute : FactAttribute
{
    public PostgresIntegrationFactAttribute()
    {
        var enabled = Environment.GetEnvironmentVariable("AUTOMAX_ENABLE_POSTGRES_INTEGRATION_TESTS");
        var conn = Environment.GetEnvironmentVariable("AUTOMAX_POSTGRES_CONNECTION_STRING");

        if (!string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(conn))
        {
            Skip = "Postgres integration tests disabled. Set AUTOMAX_ENABLE_POSTGRES_INTEGRATION_TESTS=true and AUTOMAX_POSTGRES_CONNECTION_STRING to run.";
        }
    }
}
