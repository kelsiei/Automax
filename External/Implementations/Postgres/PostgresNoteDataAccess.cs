using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.API;
using Automax.Models.Note;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresNoteDataAccess : INoteDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresNoteDataAccess> _logger;

    public PostgresNoteDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresNoteDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Note?> GetNoteAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, vehicle_id, created_at, content
                                 FROM notes
                                 WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapNote(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch note {NoteId} from Postgres.", id);
            throw;
        }
    }

    public async Task<List<Note>> GetNotesForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, vehicle_id, created_at, content
                        FROM notes
                        WHERE vehicle_id = @vid";

            var parameters = new List<NpgsqlParameter>
            {
                new("@vid", vehicleId)
            };

            if (filter?.StartDate is not null)
            {
                sql += " AND created_at >= @start";
                parameters.Add(new NpgsqlParameter("@start", filter.StartDate.Value));
            }

            if (filter?.EndDate is not null)
            {
                sql += " AND created_at <= @end";
                parameters.Add(new NpgsqlParameter("@end", filter.EndDate.Value));
            }

            sql += " ORDER BY created_at DESC";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            var results = new List<Note>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapNote(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch notes for vehicle {VehicleId} from Postgres.", vehicleId);
            throw;
        }
    }

    public async Task<int> SaveNoteAsync(Note note)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (note.Id == 0)
            {
                const string insertSql = @"INSERT INTO notes
                    (vehicle_id, created_at, content)
                    VALUES (@vehicle_id, @created_at, @content)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyNoteParameters(cmd, note);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                note.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE notes
                    SET created_at = @created_at,
                        content = @content
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyNoteParameters(cmd, note);
                cmd.Parameters.AddWithValue("@id", note.Id);

                await cmd.ExecuteNonQueryAsync();
                return note.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save note {NoteId} for vehicle {VehicleId} in Postgres.", note.Id, note.VehicleId);
            throw;
        }
    }

    public async Task DeleteNoteAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM notes WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete note {NoteId} from Postgres.", id);
            throw;
        }
    }

    private static Note MapNote(NpgsqlDataReader reader)
    {
        var content = reader.IsDBNull(reader.GetOrdinal("content")) ? string.Empty : reader.GetString(reader.GetOrdinal("content"));
        var title = string.Empty;
        var body = string.Empty;

        if (!string.IsNullOrEmpty(content))
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                title = doc.RootElement.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? string.Empty : string.Empty;
                body = doc.RootElement.TryGetProperty("body", out var bodyElement) ? bodyElement.GetString() ?? string.Empty : string.Empty;
            }
            catch (JsonException)
            {
                body = content;
            }
        }

        return new Note
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            Title = title,
            Body = body
        };
    }

    private static void ApplyNoteParameters(NpgsqlCommand cmd, Note note)
    {
        cmd.Parameters.AddWithValue("@vehicle_id", note.VehicleId);
        cmd.Parameters.AddWithValue("@created_at", note.CreatedAt);

        var content = JsonSerializer.Serialize(new
        {
            title = note.Title ?? string.Empty,
            body = note.Body ?? string.Empty
        });

        cmd.Parameters.AddWithValue("@content", content);
    }
}
