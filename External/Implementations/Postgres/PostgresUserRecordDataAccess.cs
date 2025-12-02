using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.User;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresUserRecordDataAccess : IUserRecordDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresUserRecordDataAccess> _logger;

    public PostgresUserRecordDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresUserRecordDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<UserData?> GetUserByIdAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, username, email_address, password_hash, is_admin, is_root_user
                                 FROM users
                                 WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapUser(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user {UserId} from Postgres.", id);
            throw;
        }
    }

    public async Task<UserData?> GetUserByUserNameAsync(string userName)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, username, email_address, password_hash, is_admin, is_root_user
                                 FROM users
                                 WHERE username = @username";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", userName);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapUser(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user {UserName} from Postgres.", userName);
            throw;
        }
    }

    public async Task<List<UserData>> GetAllUsersAsync()
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, username, email_address, password_hash, is_admin, is_root_user
                                 FROM users
                                 ORDER BY id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            var users = new List<UserData>();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(MapUser(reader));
            }

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch all users from Postgres.");
            throw;
        }
    }

    public async Task<int> SaveUserAsync(UserData user)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (user.Id == 0)
            {
                const string insertSql = @"INSERT INTO users
                    (username, email_address, password_hash, is_admin, is_root_user)
                    VALUES (@username, @email_address, @password_hash, @is_admin, @is_root_user)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyUserParameters(cmd, user);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                user.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE users
                    SET username = @username,
                        email_address = @email_address,
                        password_hash = @password_hash,
                        is_admin = @is_admin,
                        is_root_user = @is_root_user
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyUserParameters(cmd, user);
                cmd.Parameters.AddWithValue("@id", user.Id);

                await cmd.ExecuteNonQueryAsync();
                return user.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user {UserId} ({UserName}) in Postgres.", user.Id, user.UserName);
            throw;
        }
    }

    public async Task<bool> AnyUsersAsync()
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT EXISTS(SELECT 1 FROM users)";
            await using var cmd = new NpgsqlCommand(sql, conn);
            var result = await cmd.ExecuteScalarAsync();
            return result is bool b && b;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check for users in Postgres.");
            throw;
        }
    }

    public async Task DeleteUserAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM users WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete user {UserId} from Postgres.", id);
            throw;
        }
    }

    private static UserData MapUser(NpgsqlDataReader reader)
    {
        return new UserData
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            UserName = reader.GetString(reader.GetOrdinal("username")),
            EmailAddress = reader.IsDBNull(reader.GetOrdinal("email_address")) ? string.Empty : reader.GetString(reader.GetOrdinal("email_address")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            IsAdmin = reader.GetBoolean(reader.GetOrdinal("is_admin")),
            IsRootUser = reader.GetBoolean(reader.GetOrdinal("is_root_user"))
        };
    }

    private static void ApplyUserParameters(NpgsqlCommand cmd, UserData user)
    {
        cmd.Parameters.AddWithValue("@username", user.UserName);
        cmd.Parameters.AddWithValue("@email_address", string.IsNullOrWhiteSpace(user.EmailAddress) ? (object)DBNull.Value : user.EmailAddress);
        cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@is_admin", user.IsAdmin);
        cmd.Parameters.AddWithValue("@is_root_user", user.IsRootUser);
    }
}
