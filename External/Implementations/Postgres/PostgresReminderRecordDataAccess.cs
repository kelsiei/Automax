using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Automax.Enum;
using Automax.External.Interfaces;
using Automax.Models.API;
using Automax.Models.Reminder;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresReminderRecordDataAccess : IReminderRecordDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresReminderRecordDataAccess> _logger;

    public PostgresReminderRecordDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresReminderRecordDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<ReminderRecord?> GetReminderRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, vehicle_id, metric, description, due_date, due_odometer, urgency, is_completed, tags
                                 FROM reminder_records
                                 WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapReminder(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch reminder record {ReminderId} from Postgres.", id);
            throw;
        }
    }

    public async Task<List<ReminderRecord>> GetReminderRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, vehicle_id, metric, description, due_date, due_odometer, urgency, is_completed, tags
                        FROM reminder_records
                        WHERE vehicle_id = @vid";

            var parameters = new List<NpgsqlParameter>
            {
                new("@vid", vehicleId)
            };

            if (filter?.StartDate is not null)
            {
                sql += " AND (due_date IS NULL OR due_date >= @start)";
                parameters.Add(new NpgsqlParameter("@start", filter.StartDate.Value.Date));
            }

            if (filter?.EndDate is not null)
            {
                sql += " AND (due_date IS NULL OR due_date <= @end)";
                parameters.Add(new NpgsqlParameter("@end", filter.EndDate.Value.Date));
            }

            sql += " ORDER BY due_date, id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            var results = new List<ReminderRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapReminder(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch reminder records for vehicle {VehicleId} from Postgres.", vehicleId);
            throw;
        }
    }

    public async Task<int> SaveReminderRecordAsync(ReminderRecord record)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (record.Id == 0)
            {
                const string insertSql = @"INSERT INTO reminder_records
                    (vehicle_id, metric, description, due_date, due_odometer, urgency, is_completed, tags)
                    VALUES (@vehicle_id, @metric, @description, @due_date, @due_odometer, @urgency, @is_completed, @tags)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyReminderParameters(cmd, record);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                record.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE reminder_records
                    SET metric = @metric,
                        description = @description,
                        due_date = @due_date,
                        due_odometer = @due_odometer,
                        urgency = @urgency,
                        is_completed = @is_completed,
                        tags = @tags
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyReminderParameters(cmd, record);
                cmd.Parameters.AddWithValue("@id", record.Id);

                await cmd.ExecuteNonQueryAsync();
                return record.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save reminder record {ReminderId} for vehicle {VehicleId} in Postgres.", record.Id, record.VehicleId);
            throw;
        }
    }

    public async Task DeleteReminderRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM reminder_records WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete reminder record {ReminderId} from Postgres.", id);
            throw;
        }
    }

    private static ReminderRecord MapReminder(NpgsqlDataReader reader)
    {
        var reminder = new ReminderRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
            DueDate = reader.IsDBNull(reader.GetOrdinal("due_date")) ? null : reader.GetDateTime(reader.GetOrdinal("due_date")),
            DueOdometer = reader.IsDBNull(reader.GetOrdinal("due_odometer")) ? null : reader.GetInt32(reader.GetOrdinal("due_odometer")),
            IsCompleted = reader.GetBoolean(reader.GetOrdinal("is_completed")),
            Tags = reader.IsDBNull(reader.GetOrdinal("tags")) ? null : reader.GetString(reader.GetOrdinal("tags"))
        };

        var metricRaw = reader.IsDBNull(reader.GetOrdinal("metric")) ? null : reader.GetString(reader.GetOrdinal("metric"));
        if (!System.Enum.TryParse(metricRaw, true, out ReminderMetric metric))
        {
            metric = ReminderMetric.Date;
        }
        reminder.Metric = metric;

        var urgencyRaw = reader.IsDBNull(reader.GetOrdinal("urgency")) ? null : reader.GetString(reader.GetOrdinal("urgency"));
        if (!System.Enum.TryParse(urgencyRaw, true, out ReminderUrgency urgency))
        {
            urgency = ReminderUrgency.NotUrgent;
        }
        reminder.Urgency = urgency;

        return reminder;
    }

    private static void ApplyReminderParameters(NpgsqlCommand cmd, ReminderRecord record)
    {
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@metric", record.Metric.ToString());
        cmd.Parameters.AddWithValue("@description", record.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@due_date", record.DueDate.HasValue ? record.DueDate.Value.Date : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@due_odometer", record.DueOdometer.HasValue ? record.DueOdometer.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@urgency", record.Urgency.ToString());
        cmd.Parameters.AddWithValue("@is_completed", record.IsCompleted);
        cmd.Parameters.AddWithValue("@tags", record.Tags ?? (object)DBNull.Value);
    }
}
