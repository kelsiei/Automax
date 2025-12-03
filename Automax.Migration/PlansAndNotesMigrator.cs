using System;
using System.Text.Json;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.Models.Note;
using Automax.Models.PlanRecord;
using LiteDB;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.Migration;

internal class PlansAndNotesMigrator
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PlansAndNotesMigrator> _logger;

    public PlansAndNotesMigrator(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<PlansAndNotesMigrator> logger)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting plans + notes migration. DryRun={DryRun}", dryRun);

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        var planCollection = lite.GetCollection<PlanRecord>("plan_records");
        var noteCollection = lite.GetCollection<Note>("notes");

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var migrated = 0;

        foreach (var plan in planCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (plan.VehicleId == 0)
            {
                _logger.LogWarning("Skipping plan record {PlanId} because VehicleId is 0.", plan.Id);
                continue;
            }

            if (!await VehicleExistsAsync(conn, plan.VehicleId, cancellationToken))
            {
                _logger.LogWarning("Skipping plan record {PlanId} for vehicle {VehicleId}: vehicle not found in Postgres.", plan.Id, plan.VehicleId);
                continue;
            }

            if (await PlanExistsAsync(conn, plan.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping plan record {PlanId} (already exists).", plan.Id);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate plan record {PlanId} for vehicle {VehicleId} (Progress={Progress}, Priority={Priority}, IsArchived={IsArchived}).", plan.Id, plan.VehicleId, plan.Progress, plan.Priority, plan.IsArchived);
                migrated++;
                continue;
            }

            await InsertPlanAsync(conn, plan, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated plan record {PlanId} for vehicle {VehicleId}.", plan.Id, plan.VehicleId);
        }

        foreach (var note in noteCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (note.VehicleId == 0)
            {
                _logger.LogWarning("Skipping note {NoteId} because VehicleId is 0.", note.Id);
                continue;
            }

            if (!await VehicleExistsAsync(conn, note.VehicleId, cancellationToken))
            {
                _logger.LogWarning("Skipping note {NoteId} for vehicle {VehicleId}: vehicle not found in Postgres.", note.Id, note.VehicleId);
                continue;
            }

            if (await NoteExistsAsync(conn, note.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping note {NoteId} (already exists).", note.Id);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate note {NoteId} for vehicle {VehicleId} (Title={Title}, CreatedAt={CreatedAt}).", note.Id, note.VehicleId, note.Title, note.CreatedAt);
                migrated++;
                continue;
            }

            await InsertNoteAsync(conn, note, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated note {NoteId} for vehicle {VehicleId}.", note.Id, note.VehicleId);
        }

        if (!dryRun)
        {
            await SetSequencesAsync(conn, cancellationToken);
        }

        _logger.LogInformation("Plans + notes migration finished. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
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

    private static async Task<bool> PlanExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM plan_records WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task<bool> NoteExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM notes WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task InsertPlanAsync(NpgsqlConnection conn, PlanRecord record, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO plan_records
            (id, vehicle_id, progress, priority, title, description, is_archived, target_date)
            VALUES (@id, @vehicle_id, @progress, @priority, @title, @description, @is_archived, @target_date);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", record.Id);
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@progress", record.Progress.ToString());
        cmd.Parameters.AddWithValue("@priority", record.Priority.ToString());
        cmd.Parameters.AddWithValue("@title", DBNull.Value);
        cmd.Parameters.AddWithValue("@description", string.IsNullOrWhiteSpace(record.Description) ? (object)DBNull.Value : record.Description);
        cmd.Parameters.AddWithValue("@is_archived", record.IsArchived);
        cmd.Parameters.AddWithValue("@target_date", record.PlannedDate.HasValue ? record.PlannedDate.Value.Date : (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertNoteAsync(NpgsqlConnection conn, Note record, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO notes
            (id, vehicle_id, created_at, content)
            VALUES (@id, @vehicle_id, @created_at, @content);";

        var content = System.Text.Json.JsonSerializer.Serialize(new
        {
            title = record.Title ?? string.Empty,
            body = record.Body ?? string.Empty
        });

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", record.Id);
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@created_at", record.CreatedAt);
        cmd.Parameters.AddWithValue("@content", content);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SetSequencesAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        const string planSql = @"SELECT setval(pg_get_serial_sequence('plan_records','id'), (SELECT COALESCE(MAX(id),0) FROM plan_records))";
        const string noteSql = @"SELECT setval(pg_get_serial_sequence('notes','id'), (SELECT COALESCE(MAX(id),0) FROM notes))";

        await using (var cmd = new NpgsqlCommand(planSql, conn))
        {
            await cmd.ExecuteScalarAsync(cancellationToken);
        }

        await using (var cmd = new NpgsqlCommand(noteSql, conn))
        {
            await cmd.ExecuteScalarAsync(cancellationToken);
        }
    }
}
