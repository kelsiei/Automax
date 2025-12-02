using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.API;
using Automax.Models.ServiceRecord;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresServiceRecordDataAccess : IServiceRecordDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresServiceRecordDataAccess> _logger;

    public PostgresServiceRecordDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresServiceRecordDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<ServiceRecord?> GetServiceRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, vehicle_id, date, cost, odometer, description, notes
                                 FROM service_records
                                 WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapService(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch service record {ServiceRecordId} from Postgres.", id);
            throw;
        }
    }

    public async Task<List<ServiceRecord>> GetServiceRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, vehicle_id, date, cost, odometer, description, notes
                        FROM service_records
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

            var results = new List<ServiceRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapService(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch service records for vehicle {VehicleId} from Postgres.", vehicleId);
            throw;
        }
    }

    public async Task<int> SaveServiceRecordAsync(ServiceRecord record)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (record.Id == 0)
            {
                const string insertSql = @"INSERT INTO service_records
                    (vehicle_id, date, cost, odometer, description, notes)
                    VALUES (@vehicle_id, @date, @cost, @odometer, @description, @notes)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyServiceParameters(cmd, record);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                record.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE service_records
                    SET date = @date,
                        cost = @cost,
                        odometer = @odometer,
                        description = @description,
                        notes = @notes
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyServiceParameters(cmd, record);
                cmd.Parameters.AddWithValue("@id", record.Id);

                await cmd.ExecuteNonQueryAsync();
                return record.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save service record {ServiceRecordId} for vehicle {VehicleId} in Postgres.", record.Id, record.VehicleId);
            throw;
        }
    }

    public async Task DeleteServiceRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM service_records WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete service record {ServiceRecordId} from Postgres.", id);
            throw;
        }
    }

    private static ServiceRecord MapService(NpgsqlDataReader reader)
    {
        return new ServiceRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id")),
            Date = reader.GetDateTime(reader.GetOrdinal("date")),
            Cost = reader.IsDBNull(reader.GetOrdinal("cost")) ? null : reader.GetDecimal(reader.GetOrdinal("cost")),
            Odometer = reader.IsDBNull(reader.GetOrdinal("odometer")) ? null : reader.GetInt32(reader.GetOrdinal("odometer")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
            Vendor = null
        };
    }

    private static void ApplyServiceParameters(NpgsqlCommand cmd, ServiceRecord record)
    {
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@date", record.Date);
        cmd.Parameters.AddWithValue("@cost", record.Cost.HasValue ? record.Cost.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@odometer", record.Odometer.HasValue ? record.Odometer.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@description", record.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", DBNull.Value);
    }
}
