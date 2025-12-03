using System;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.Models.Settings;
using Automax.Models.User;
using LiteDB;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.Migration;

internal class UsersAccessConfigsMigrator
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<UsersAccessConfigsMigrator> _logger;

    public UsersAccessConfigsMigrator(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<UsersAccessConfigsMigrator> logger)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting users + access + configs migration. DryRun={DryRun}", dryRun);

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        var userCollection = lite.GetCollection<UserData>("users");
        var accessCollection = lite.GetCollection<UserAccess>("user_access");
        var configCollection = lite.GetCollection<UserConfigData>("user_configs");

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var migrated = 0;

        // Users
        foreach (var user in userCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (user.Id == 0)
            {
                _logger.LogWarning("Skipping user with Id 0.");
                continue;
            }

            if (await UserExistsAsync(conn, user.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping user {UserId} ({UserName}) (already exists).", user.Id, user.UserName);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate user {UserId} ({UserName}) IsAdmin={IsAdmin} IsRootUser={IsRootUser}.", user.Id, user.UserName, user.IsAdmin, user.IsRootUser);
                migrated++;
                continue;
            }

            await InsertUserAsync(conn, user, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated user {UserId} ({UserName}).", user.Id, user.UserName);
        }

        // User configs
        foreach (var configData in configCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (configData.UserId == 0)
            {
                _logger.LogWarning("Skipping user config because UserId is 0.");
                continue;
            }

            if (!await UserExistsAsync(conn, configData.UserId, cancellationToken))
            {
                _logger.LogWarning("Skipping user config for user {UserId}: user not found in Postgres.", configData.UserId);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate user config for user {UserId} (HasConfig={HasConfig}).", configData.UserId, configData.Config != null);
                migrated++;
                continue;
            }

            await UpsertUserConfigAsync(conn, configData, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated user config for user {UserId}.", configData.UserId);
        }

        // User access
        foreach (var access in accessCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (access.UserId == 0)
            {
                _logger.LogWarning("Skipping user access {AccessId} because UserId is 0.", access.Id);
                continue;
            }

            if (!await UserExistsAsync(conn, access.UserId, cancellationToken))
            {
                _logger.LogWarning("Skipping user access {AccessId} for user {UserId}: user not found in Postgres.", access.Id, access.UserId);
                continue;
            }

            if (access.VehicleId == 0)
            {
                _logger.LogWarning("Skipping user access {AccessId} because VehicleId is 0.", access.Id);
                continue;
            }

            if (!await VehicleExistsAsync(conn, access.VehicleId, cancellationToken))
            {
                _logger.LogWarning("Skipping user access {AccessId} for vehicle {VehicleId}: vehicle not found in Postgres.", access.Id, access.VehicleId);
                continue;
            }

            if (await AccessExistsAsync(conn, access.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping user access {AccessId} (already exists).", access.Id);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate user access {AccessId} (UserId={UserId}, VehicleId={VehicleId}, CanEdit={CanEdit}).", access.Id, access.UserId, access.VehicleId, access.CanEdit);
                migrated++;
                continue;
            }

            await InsertAccessAsync(conn, access, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated user access {AccessId} (UserId={UserId}, VehicleId={VehicleId}).", access.Id, access.UserId, access.VehicleId);
        }

        if (!dryRun)
        {
            await SetSequencesAsync(conn, cancellationToken);
        }

        _logger.LogInformation("Users + access + configs migration finished. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
        return migrated;
    }

    private static async Task<bool> UserExistsAsync(NpgsqlConnection conn, int userId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM users WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", userId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task<bool> VehicleExistsAsync(NpgsqlConnection conn, int vehicleId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM vehicles WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", vehicleId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task<bool> AccessExistsAsync(NpgsqlConnection conn, int accessId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM user_access WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", accessId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task InsertUserAsync(NpgsqlConnection conn, UserData user, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO users
            (id, username, email_address, password_hash, is_admin, is_root_user)
            VALUES (@id, @username, @email_address, @password_hash, @is_admin, @is_root_user);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", user.Id);
        cmd.Parameters.AddWithValue("@username", user.UserName);
        cmd.Parameters.AddWithValue("@email_address", string.IsNullOrWhiteSpace(user.EmailAddress) ? (object)DBNull.Value : user.EmailAddress);
        cmd.Parameters.AddWithValue("@password_hash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@is_admin", user.IsAdmin);
        cmd.Parameters.AddWithValue("@is_root_user", user.IsRootUser);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertUserConfigAsync(NpgsqlConnection conn, UserConfigData configData, CancellationToken cancellationToken)
    {
        var rawJson = configData.Config is null
            ? "{}"
            : System.Text.Json.JsonSerializer.Serialize(configData.Config);

        const string updateSql = @"UPDATE user_configs
                                   SET raw_json = @raw_json
                                   WHERE user_id = @uid";

        await using var updateCmd = new NpgsqlCommand(updateSql, conn);
        updateCmd.Parameters.AddWithValue("@raw_json", rawJson);
        updateCmd.Parameters.AddWithValue("@uid", configData.UserId);

        var affected = await updateCmd.ExecuteNonQueryAsync(cancellationToken);
        if (affected > 0)
        {
            return;
        }

        const string insertSql = @"INSERT INTO user_configs (user_id, raw_json)
                                   VALUES (@uid, @raw_json)";

        await using var insertCmd = new NpgsqlCommand(insertSql, conn);
        insertCmd.Parameters.AddWithValue("@uid", configData.UserId);
        insertCmd.Parameters.AddWithValue("@raw_json", rawJson);
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertAccessAsync(NpgsqlConnection conn, UserAccess access, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO user_access
            (id, user_id, vehicle_id, can_edit)
            VALUES (@id, @user_id, @vehicle_id, @can_edit);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", access.Id);
        cmd.Parameters.AddWithValue("@user_id", access.UserId);
        cmd.Parameters.AddWithValue("@vehicle_id", access.VehicleId);
        cmd.Parameters.AddWithValue("@can_edit", access.CanEdit);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SetSequencesAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        const string usersSql = @"SELECT setval(pg_get_serial_sequence('users','id'), (SELECT COALESCE(MAX(id),0) FROM users))";
        const string accessSql = @"SELECT setval(pg_get_serial_sequence('user_access','id'), (SELECT COALESCE(MAX(id),0) FROM user_access))";

        await using (var cmd = new NpgsqlCommand(usersSql, conn))
        {
            await cmd.ExecuteScalarAsync(cancellationToken);
        }

        await using (var cmd = new NpgsqlCommand(accessSql, conn))
        {
            await cmd.ExecuteScalarAsync(cancellationToken);
        }
    }
}
