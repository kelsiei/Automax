using System;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.Models.Shared;
using LiteDB;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.Migration;

internal class ExtraFieldsMigrator
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<ExtraFieldsMigrator> _logger;

    public ExtraFieldsMigrator(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<ExtraFieldsMigrator> logger)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting extra fields migration. DryRun={DryRun}", dryRun);

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        var collection = lite.GetCollection<RecordExtraField>("extra_fields");

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var migrated = 0;

        foreach (var field in collection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (field.Id == 0)
            {
                _logger.LogWarning("Skipping extra field with Id 0 (Name={Name}, RecordId={RecordId}).", field.Name, field.RecordId);
                continue;
            }

            if (field.RecordId == 0)
            {
                _logger.LogWarning("Skipping extra field {Id} because RecordId is 0 (Name={Name}).", field.Id, field.Name);
                continue;
            }

            if (await ExtraFieldExistsAsync(conn, field.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping extra field {Id} (already exists) (Name={Name}).", field.Id, field.Name);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate extra field {Id} for record {RecordId} (Name={Name}, Type={Type}).", field.Id, field.RecordId, field.Name, field.Type);
                migrated++;
                continue;
            }

            await InsertExtraFieldAsync(conn, field, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated extra field {Id} for record {RecordId} (Name={Name}).", field.Id, field.RecordId, field.Name);
        }

        if (!dryRun)
        {
            await SetSequenceAsync(conn, cancellationToken);
        }

        _logger.LogInformation("Extra fields migration finished. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
        return migrated;
    }

    private static async Task<bool> ExtraFieldExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM record_extra_fields WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task InsertExtraFieldAsync(NpgsqlConnection conn, RecordExtraField field, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO record_extra_fields
            (id, record_id, name, value, type)
            VALUES (@id, @record_id, @name, @value, @type);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", field.Id);
        cmd.Parameters.AddWithValue("@record_id", field.RecordId);
        cmd.Parameters.AddWithValue("@name", field.Name ?? string.Empty);
        cmd.Parameters.AddWithValue("@value", string.IsNullOrWhiteSpace(field.Value) ? (object)DBNull.Value : field.Value);
        cmd.Parameters.AddWithValue("@type", field.Type.ToString());

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SetSequenceAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT setval(pg_get_serial_sequence('record_extra_fields','id'), (SELECT COALESCE(MAX(id),0) FROM record_extra_fields))";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteScalarAsync(cancellationToken);
    }
}
