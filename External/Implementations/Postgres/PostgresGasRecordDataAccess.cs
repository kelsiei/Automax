using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.API;
using Automax.Models.GasRecord;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresGasRecordDataAccess : IGasRecordDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresGasRecordDataAccess> _logger;

    public PostgresGasRecordDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresGasRecordDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<GasRecord?> GetGasRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, vehicle_id, date, volume, total_cost, odometer, notes
                                 FROM gas_records
                                 WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapGas(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch gas record {GasRecordId} from Postgres.", id);
            throw;
        }
    }

    public async Task<List<GasRecord>> GetGasRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, vehicle_id, date, volume, total_cost, odometer, notes
                        FROM gas_records
                        WHERE vehicle_id = @vid";

            var parameters = new List<NpgsqlParameter>
            {
                new("@vid", vehicleId)
            };

            if (filter?.StartDate is not null)
            {
                sql += " AND date >= @start";
                parameters.Add(new NpgsqlParameter("@start", filter.StartDate.Value));
            }

            if (filter?.EndDate is not null)
            {
                sql += " AND date <= @end";
                parameters.Add(new NpgsqlParameter("@end", filter.EndDate.Value));
            }

            sql += " ORDER BY date";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            var results = new List<GasRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapGas(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch gas records for vehicle {VehicleId} from Postgres.", vehicleId);
            throw;
        }
    }

    public async Task<int> SaveGasRecordAsync(GasRecord record)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (record.Id == 0)
            {
                const string insertSql = @"INSERT INTO gas_records
                    (vehicle_id, date, volume, total_cost, odometer, notes)
                    VALUES (@vehicle_id, @date, @volume, @total_cost, @odometer, @notes)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyGasParameters(cmd, record);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                record.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE gas_records
                    SET date = @date,
                        volume = @volume,
                        total_cost = @total_cost,
                        odometer = @odometer,
                        notes = @notes
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyGasParameters(cmd, record);
                cmd.Parameters.AddWithValue("@id", record.Id);

                await cmd.ExecuteNonQueryAsync();
                return record.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save gas record {GasRecordId} for vehicle {VehicleId} in Postgres.", record.Id, record.VehicleId);
            throw;
        }
    }

    public async Task DeleteGasRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM gas_records WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete gas record {GasRecordId} from Postgres.", id);
            throw;
        }
    }

    private static GasRecord MapGas(NpgsqlDataReader reader)
    {
        return new GasRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id")),
            Date = reader.GetDateTime(reader.GetOrdinal("date")),
            Volume = reader.IsDBNull(reader.GetOrdinal("volume")) ? 0m : reader.GetDecimal(reader.GetOrdinal("volume")),
            TotalCost = reader.IsDBNull(reader.GetOrdinal("total_cost")) ? 0m : reader.GetDecimal(reader.GetOrdinal("total_cost")),
            Odometer = reader.IsDBNull(reader.GetOrdinal("odometer")) ? null : reader.GetInt32(reader.GetOrdinal("odometer")),
            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
        };
    }

    private static void ApplyGasParameters(NpgsqlCommand cmd, GasRecord record)
    {
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@date", record.Date);
        cmd.Parameters.AddWithValue("@volume", record.Volume);
        cmd.Parameters.AddWithValue("@total_cost", record.TotalCost);
        cmd.Parameters.AddWithValue("@odometer", record.Odometer.HasValue ? record.Odometer.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", record.Notes ?? (object)DBNull.Value);
    }
}
