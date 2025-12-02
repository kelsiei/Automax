using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.User;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresUserAccessDataAccess : IUserAccessDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresUserAccessDataAccess> _logger;

    public PostgresUserAccessDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresUserAccessDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<List<UserAccess>> GetUserAccessForUserAsync(int userId)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, user_id, vehicle_id, can_edit
                                 FROM user_access
                                 WHERE user_id = @uid";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);

            var results = new List<UserAccess>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapAccess(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user access entries for user {UserId} from Postgres.", userId);
            throw;
        }
    }

    public async Task<List<UserAccess>> GetUserAccessForVehicleAsync(int vehicleId)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, user_id, vehicle_id, can_edit
                                 FROM user_access
                                 WHERE vehicle_id = @vid";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@vid", vehicleId);

            var results = new List<UserAccess>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapAccess(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user access entries for vehicle {VehicleId} from Postgres.", vehicleId);
            throw;
        }
    }

    public async Task<int> SaveUserAccessAsync(UserAccess access)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"INSERT INTO user_access (user_id, vehicle_id, can_edit)
                                 VALUES (@user_id, @vehicle_id, @can_edit)
                                 ON CONFLICT (user_id, vehicle_id) DO UPDATE
                                 SET can_edit = EXCLUDED.can_edit
                                 RETURNING id;";

            await using var cmd = new NpgsqlCommand(sql, conn);
            ApplyAccessParameters(cmd, access);

            var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
            access.Id = newId;
            return newId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user access for user {UserId} vehicle {VehicleId} in Postgres.", access.UserId, access.VehicleId);
            throw;
        }
    }

    public async Task DeleteUserAccessAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM user_access WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user access {AccessId} from Postgres.", id);
            throw;
        }
    }

    private static UserAccess MapAccess(NpgsqlDataReader reader)
    {
        return new UserAccess
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            UserId = reader.GetInt32(reader.GetOrdinal("user_id")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id")),
            CanEdit = reader.GetBoolean(reader.GetOrdinal("can_edit"))
        };
    }

    private static void ApplyAccessParameters(NpgsqlCommand cmd, UserAccess access)
    {
        cmd.Parameters.AddWithValue("@user_id", access.UserId);
        cmd.Parameters.AddWithValue("@vehicle_id", access.VehicleId);
        cmd.Parameters.AddWithValue("@can_edit", access.CanEdit);
    }
}
