using Automax.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

/// <summary>
/// Creates Npgsql connections using configuration from ServerConfig.
/// </summary>
public class PostgresConnectionFactory
{
    private readonly ILogger<PostgresConnectionFactory> _logger;
    private readonly string? _connectionString;

    public PostgresConnectionFactory(IOptions<ServerConfig> configOptions, ILogger<PostgresConnectionFactory> logger)
    {
        _logger = logger;
        _connectionString = configOptions.Value.PostgresConnectionString;
    }

    /// <summary>
    /// Returns a new unopened NpgsqlConnection. Throws if the connection string is missing.
    /// </summary>
    public NpgsqlConnection CreateConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            _logger.LogError("Postgres connection string is not configured.");
            throw new NotSupportedException("Postgres connection string is missing. Configure ServerConfig.PostgresConnectionString.");
        }

        return new NpgsqlConnection(_connectionString);
    }
}
