using System;
using System.Text.Json;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.Settings;
using Automax.Models.User;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresUserConfigDataAccess : IUserConfigDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresUserConfigDataAccess> _logger;

    public PostgresUserConfigDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresUserConfigDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<UserConfigData?> GetUserConfigAsync(int userId)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT raw_json
                                 FROM user_configs
                                 WHERE user_id = @uid";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            if (reader.IsDBNull(reader.GetOrdinal("raw_json")))
            {
                return null;
            }

            var rawJson = reader.GetString(reader.GetOrdinal("raw_json"));
            var config = JsonSerializer.Deserialize<UserConfig>(rawJson);

            return new UserConfigData
            {
                UserId = userId,
                Config = config
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user config for user {UserId} from Postgres.", userId);
            throw;
        }
    }

    public async Task SaveUserConfigAsync(int userId, UserConfig config)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            var rawJson = JsonSerializer.Serialize(config);

            const string updateSql = @"UPDATE user_configs
                                       SET raw_json = @raw_json
                                       WHERE user_id = @uid";

            await using var updateCmd = new NpgsqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@raw_json", rawJson);
            updateCmd.Parameters.AddWithValue("@uid", userId);

            var affected = await updateCmd.ExecuteNonQueryAsync();
            if (affected > 0)
            {
                return;
            }

            const string insertSql = @"INSERT INTO user_configs (user_id, raw_json)
                                       VALUES (@uid, @raw_json)";

            await using var insertCmd = new NpgsqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@uid", userId);
            insertCmd.Parameters.AddWithValue("@raw_json", rawJson);
            await insertCmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user config for user {UserId} in Postgres.", userId);
            throw;
        }
    }
}
