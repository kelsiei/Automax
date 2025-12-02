using System;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.Models.Document;
using LiteDB;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.Migration;

internal class DocumentsMigrator
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<DocumentsMigrator> _logger;

    public DocumentsMigrator(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<DocumentsMigrator> logger)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting documents metadata migration. DryRun={DryRun}", dryRun);

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        var docCollection = lite.GetCollection<DocumentMetadata>("documents");

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var migrated = 0;

        foreach (var doc in docCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (doc.Id == 0)
            {
                _logger.LogWarning("Skipping document with Id 0 (FileName={FileName}, VehicleId={VehicleId}).", doc.FileName, doc.VehicleId);
                continue;
            }

            if (doc.VehicleId == 0)
            {
                _logger.LogWarning("Skipping document {DocId} because VehicleId is 0 (FileName={FileName}).", doc.Id, doc.FileName);
                continue;
            }

            if (!await VehicleExistsAsync(conn, doc.VehicleId, cancellationToken))
            {
                _logger.LogWarning("Skipping document {DocId} for vehicle {VehicleId}: vehicle not found in Postgres (FileName={FileName}).", doc.Id, doc.VehicleId, doc.FileName);
                continue;
            }

            if (await DocumentExistsAsync(conn, doc.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping document {DocId} (already exists) (FileName={FileName}).", doc.Id, doc.FileName);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate document {DocId} for vehicle {VehicleId} (FileName={FileName}, FilePath={FilePath}, UploadedAt={UploadedAt}).",
                    doc.Id, doc.VehicleId, doc.FileName, doc.FilePath, doc.UploadedAt);
                migrated++;
                continue;
            }

            await InsertDocumentAsync(conn, doc, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated document {DocId} for vehicle {VehicleId} (FileName={FileName}).", doc.Id, doc.VehicleId, doc.FileName);
        }

        if (!dryRun)
        {
            await SetSequencesAsync(conn, cancellationToken);
        }

        _logger.LogInformation("Documents migration finished. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
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

    private static async Task<bool> DocumentExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM documents WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task InsertDocumentAsync(NpgsqlConnection conn, DocumentMetadata doc, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO documents
            (id, vehicle_id, file_name, file_path, uploaded_at)
            VALUES (@id, @vehicle_id, @file_name, @file_path, @uploaded_at);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", doc.Id);
        cmd.Parameters.AddWithValue("@vehicle_id", doc.VehicleId);
        cmd.Parameters.AddWithValue("@file_name", string.IsNullOrWhiteSpace(doc.FileName) ? (object)DBNull.Value : doc.FileName);
        cmd.Parameters.AddWithValue("@file_path", string.IsNullOrWhiteSpace(doc.FilePath) ? (object)DBNull.Value : doc.FilePath);
        cmd.Parameters.AddWithValue("@uploaded_at", doc.UploadedAt);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SetSequencesAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT setval(pg_get_serial_sequence('documents','id'), (SELECT COALESCE(MAX(id),0) FROM documents))";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteScalarAsync(cancellationToken);
    }
}
