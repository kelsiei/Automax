using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.Shared;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresExtraFieldDataAccess : IExtraFieldDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresExtraFieldDataAccess> _logger;

    public PostgresExtraFieldDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresExtraFieldDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<List<RecordExtraField>> GetExtraFieldsForRecordAsync(int recordId)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, record_id, name, value, type
                                 FROM record_extra_fields
                                 WHERE record_id = @record_id
                                 ORDER BY id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@record_id", recordId);

            var results = new List<RecordExtraField>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapExtraField(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch extra fields for record {RecordId} from Postgres.", recordId);
            throw;
        }
    }

    public async Task<int> SaveExtraFieldAsync(RecordExtraField extraField)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (extraField.Id == 0)
            {
                const string insertSql = @"INSERT INTO record_extra_fields
                    (record_id, name, value, type)
                    VALUES (@record_id, @name, @value, @type)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyParameters(cmd, extraField);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                extraField.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE record_extra_fields
                    SET record_id = @record_id,
                        name = @name,
                        value = @value,
                        type = @type
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyParameters(cmd, extraField);
                cmd.Parameters.AddWithValue("@id", extraField.Id);

                await cmd.ExecuteNonQueryAsync();
                return extraField.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save extra field {ExtraFieldId} for record {RecordId} in Postgres.", extraField.Id, extraField.RecordId);
            throw;
        }
    }

    public async Task DeleteExtraFieldAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM record_extra_fields WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete extra field {ExtraFieldId} from Postgres.", id);
            throw;
        }
    }

    private static RecordExtraField MapExtraField(NpgsqlDataReader reader)
    {
        var typeRaw = reader.IsDBNull(reader.GetOrdinal("type")) ? null : reader.GetString(reader.GetOrdinal("type"));
        if (!System.Enum.TryParse(typeRaw, true, out Automax.Enum.ExtraFieldType type))
        {
            type = Automax.Enum.ExtraFieldType.Text;
        }

        return new RecordExtraField
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            RecordId = reader.GetInt32(reader.GetOrdinal("record_id")),
            Name = reader.IsDBNull(reader.GetOrdinal("name")) ? string.Empty : reader.GetString(reader.GetOrdinal("name")),
            Value = reader.IsDBNull(reader.GetOrdinal("value")) ? null : reader.GetString(reader.GetOrdinal("value")),
            Type = type
        };
    }

    private static void ApplyParameters(NpgsqlCommand cmd, RecordExtraField extraField)
    {
        cmd.Parameters.AddWithValue("@record_id", extraField.RecordId);
        cmd.Parameters.AddWithValue("@name", extraField.Name ?? string.Empty);
        cmd.Parameters.AddWithValue("@value", string.IsNullOrWhiteSpace(extraField.Value) ? (object)DBNull.Value : extraField.Value);
        cmd.Parameters.AddWithValue("@type", extraField.Type.ToString());
    }
}
