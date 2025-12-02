using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.Document;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresDocumentDataAccess : IDocumentDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresDocumentDataAccess> _logger;

    public PostgresDocumentDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresDocumentDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DocumentMetadata>> GetDocumentsForVehicleAsync(int vehicleId)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, vehicle_id, file_name, file_path, uploaded_at
                                 FROM documents
                                 WHERE vehicle_id = @vid
                                 ORDER BY uploaded_at DESC, id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@vid", vehicleId);

            var results = new List<DocumentMetadata>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapDocument(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch documents for vehicle {VehicleId} from Postgres.", vehicleId);
            throw;
        }
    }

    public async Task<int> SaveDocumentMetadataAsync(DocumentMetadata metadata)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (metadata.Id == 0)
            {
                const string insertSql = @"INSERT INTO documents
                    (vehicle_id, file_name, file_path, uploaded_at)
                    VALUES (@vehicle_id, @file_name, @file_path, @uploaded_at)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyParameters(cmd, metadata);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                metadata.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE documents
                    SET vehicle_id = @vehicle_id,
                        file_name = @file_name,
                        file_path = @file_path,
                        uploaded_at = @uploaded_at
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyParameters(cmd, metadata);
                cmd.Parameters.AddWithValue("@id", metadata.Id);

                await cmd.ExecuteNonQueryAsync();
                return metadata.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save document metadata {DocumentId} for vehicle {VehicleId} in Postgres.", metadata.Id, metadata.VehicleId);
            throw;
        }
    }

    public async Task DeleteDocumentMetadataAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM documents WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete document metadata {DocumentId} from Postgres.", id);
            throw;
        }
    }

    private static DocumentMetadata MapDocument(NpgsqlDataReader reader)
    {
        return new DocumentMetadata
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id")),
            FileName = reader.GetString(reader.GetOrdinal("file_name")),
            FilePath = reader.GetString(reader.GetOrdinal("file_path")),
            UploadedAt = reader.GetDateTime(reader.GetOrdinal("uploaded_at"))
        };
    }

    private static void ApplyParameters(NpgsqlCommand cmd, DocumentMetadata metadata)
    {
        cmd.Parameters.AddWithValue("@vehicle_id", metadata.VehicleId);
        cmd.Parameters.AddWithValue("@file_name", metadata.FileName);
        cmd.Parameters.AddWithValue("@file_path", metadata.FilePath);
        cmd.Parameters.AddWithValue("@uploaded_at", metadata.UploadedAt);
    }
}
