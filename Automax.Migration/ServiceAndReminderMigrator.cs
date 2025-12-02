using System;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.Models.Reminder;
using Automax.Models.ServiceRecord;
using LiteDB;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.Migration;

internal class ServiceAndReminderMigrator
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<ServiceAndReminderMigrator> _logger;

    public ServiceAndReminderMigrator(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<ServiceAndReminderMigrator> logger)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting service + reminder migration. DryRun={DryRun}", dryRun);

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        var serviceCollection = lite.GetCollection<ServiceRecord>("service_records");
        var reminderCollection = lite.GetCollection<ReminderRecord>("reminder_records");

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var migrated = 0;

        foreach (var record in serviceCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (record.VehicleId == 0)
            {
                _logger.LogWarning("Skipping service record {ServiceId} because VehicleId is 0.", record.Id);
                continue;
            }

            if (!await VehicleExistsAsync(conn, record.VehicleId, cancellationToken))
            {
                _logger.LogWarning("Skipping service record {ServiceId} for vehicle {VehicleId}: vehicle not found in Postgres.", record.Id, record.VehicleId);
                continue;
            }

            if (await ServiceExistsAsync(conn, record.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping service record {ServiceId} (already exists).", record.Id);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate service record {ServiceId} for vehicle {VehicleId} (Date={Date}, Cost={Cost}).", record.Id, record.VehicleId, record.Date, record.Cost);
                migrated++;
                continue;
            }

            await InsertServiceAsync(conn, record, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated service record {ServiceId} for vehicle {VehicleId}.", record.Id, record.VehicleId);
        }

        foreach (var record in reminderCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (record.VehicleId == 0)
            {
                _logger.LogWarning("Skipping reminder record {ReminderId} because VehicleId is 0.", record.Id);
                continue;
            }

            if (!await VehicleExistsAsync(conn, record.VehicleId, cancellationToken))
            {
                _logger.LogWarning("Skipping reminder record {ReminderId} for vehicle {VehicleId}: vehicle not found in Postgres.", record.Id, record.VehicleId);
                continue;
            }

            if (await ReminderExistsAsync(conn, record.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping reminder record {ReminderId} (already exists).", record.Id);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate reminder record {ReminderId} for vehicle {VehicleId} (Metric={Metric}, DueDate={DueDate}, DueOdometer={DueOdometer}, Urgency={Urgency}, IsCompleted={IsCompleted}).",
                    record.Id, record.VehicleId, record.Metric, record.DueDate, record.DueOdometer, record.Urgency, record.IsCompleted);
                migrated++;
                continue;
            }

            await InsertReminderAsync(conn, record, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated reminder record {ReminderId} for vehicle {VehicleId}.", record.Id, record.VehicleId);
        }

        if (!dryRun)
        {
            await SetSequencesAsync(conn, cancellationToken);
        }

        _logger.LogInformation("Service + reminder migration finished. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
        return migrated;
    }

    private static async Task<bool> VehicleExistsAsync(NpgsqlConnection conn, int vehicleId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM vehicles WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", vehicleId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task<bool> ServiceExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM service_records WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task<bool> ReminderExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM reminder_records WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task InsertServiceAsync(NpgsqlConnection conn, ServiceRecord record, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO service_records
            (id, vehicle_id, date, cost, odometer, description, notes)
            VALUES (@id, @vehicle_id, @date, @cost, @odometer, @description, @notes);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", record.Id);
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@date", record.Date.Date);
        cmd.Parameters.AddWithValue("@cost", record.Cost.HasValue ? record.Cost.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@odometer", record.Odometer.HasValue ? record.Odometer.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(record.Description) ? (object)DBNull.Value : record.Description);
        cmd.Parameters.AddWithValue("@notes", DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertReminderAsync(NpgsqlConnection conn, ReminderRecord record, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO reminder_records
            (id, vehicle_id, metric, description, due_date, due_odometer, urgency, is_completed, tags)
            VALUES (@id, @vehicle_id, @metric, @description, @due_date, @due_odometer, @urgency, @is_completed, @tags);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", record.Id);
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@metric", record.Metric.ToString());
        cmd.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(record.Description) ? (object)DBNull.Value : record.Description);
        cmd.Parameters.AddWithValue("@due_date", record.DueDate.HasValue ? record.DueDate.Value.Date : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@due_odometer", record.DueOdometer.HasValue ? record.DueOdometer.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@urgency", record.Urgency.ToString());
        cmd.Parameters.AddWithValue("@is_completed", record.IsCompleted);
        cmd.Parameters.AddWithValue("@tags", string.IsNullOrWhiteSpace(record.Tags) ? (object)DBNull.Value : record.Tags);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SetSequencesAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        const string serviceSql = @"SELECT setval(pg_get_serial_sequence('service_records','id'), (SELECT COALESCE(MAX(id),0) FROM service_records))";
        const string reminderSql = @"SELECT setval(pg_get_serial_sequence('reminder_records','id'), (SELECT COALESCE(MAX(id),0) FROM reminder_records))";

        await using (var cmd = new NpgsqlCommand(serviceSql, conn))
        {
            await cmd.ExecuteScalarAsync(cancellationToken);
        }

        await using (var cmd = new NpgsqlCommand(reminderSql, conn))
        {
            await cmd.ExecuteScalarAsync(cancellationToken);
        }
    }
}
