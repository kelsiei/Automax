using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.API;
using Automax.Models.OdometerRecord;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresOdometerDataAccess : IOdometerRecordDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresOdometerDataAccess> _logger;

    public PostgresOdometerDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresOdometerDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<OdometerRecord?> GetOdometerRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, vehicle_id, date_recorded, odometer, notes
                                 FROM odometer_records
                                 WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapOdometer(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch odometer record {OdometerId} from Postgres.", id);
            throw;
        }
    }

    public async Task<List<OdometerRecord>> GetOdometerRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, vehicle_id, date_recorded, odometer, notes
                        FROM odometer_records
                        WHERE vehicle_id = @vid";

            var parameters = new List<NpgsqlParameter>
            {
                new("@vid", vehicleId)
            };

            if (filter?.StartDate is not null)
            {
                sql += " AND date_recorded >= @start";
                parameters.Add(new NpgsqlParameter("@start", filter.StartDate.Value));
            }

            if (filter?.EndDate is not null)
            {
                sql += " AND date_recorded <= @end";
                parameters.Add(new NpgsqlParameter("@end", filter.EndDate.Value));
            }

            sql += " ORDER BY date_recorded";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            var results = new List<OdometerRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapOdometer(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch odometer records for vehicle {VehicleId} from Postgres.", vehicleId);
            throw;
        }
    }

    public async Task<int> SaveOdometerRecordAsync(OdometerRecord record)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (record.Id == 0)
            {
                const string insertSql = @"INSERT INTO odometer_records
                    (vehicle_id, date_recorded, odometer, notes)
                    VALUES (@vehicle_id, @date_recorded, @odometer, @notes)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyOdometerParameters(cmd, record);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                record.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE odometer_records
                    SET date_recorded = @date_recorded,
                        odometer = @odometer,
                        notes = @notes
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyOdometerParameters(cmd, record);
                cmd.Parameters.AddWithValue("@id", record.Id);

                await cmd.ExecuteNonQueryAsync();
                return record.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save odometer record {OdometerId} for vehicle {VehicleId} in Postgres.", record.Id, record.VehicleId);
            throw;
        }
    }

    public async Task DeleteOdometerRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM odometer_records WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete odometer record {OdometerId} from Postgres.", id);
            throw;
        }
    }

    private static OdometerRecord MapOdometer(NpgsqlDataReader reader)
    {
        return new OdometerRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id")),
            Date = reader.GetDateTime(reader.GetOrdinal("date_recorded")),
            Odometer = reader.GetInt32(reader.GetOrdinal("odometer")),
            Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? null : reader.GetString(reader.GetOrdinal("notes"))
        };
    }

    private static void ApplyOdometerParameters(NpgsqlCommand cmd, OdometerRecord record)
    {
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@date_recorded", record.Date);
        cmd.Parameters.AddWithValue("@odometer", record.Odometer);
        cmd.Parameters.AddWithValue("@notes", record.Notes ?? (object)DBNull.Value);
    }
}
