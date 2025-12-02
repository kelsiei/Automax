using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automax.External.Interfaces;
using Automax.Models.API;
using Automax.Models.PlanRecord;
using Automax.Enum;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

public class PostgresPlanRecordDataAccess : IPlanRecordDataAccess
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<PostgresPlanRecordDataAccess> _logger;

    public PostgresPlanRecordDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresPlanRecordDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<PlanRecord?> GetPlanRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, vehicle_id, progress, priority, title, description, is_archived, target_date
                                 FROM plan_records
                                 WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapPlan(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch plan record {PlanRecordId} from Postgres.", id);
            throw;
        }
    }

    public async Task<List<PlanRecord>> GetPlanRecordsForVehicleAsync(int vehicleId, MethodParameter? filter = null)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            var sql = @"SELECT id, vehicle_id, progress, priority, title, description, is_archived, target_date
                        FROM plan_records
                        WHERE vehicle_id = @vid";

            var parameters = new List<NpgsqlParameter>
            {
                new("@vid", vehicleId)
            };

            if (filter?.StartDate is not null)
            {
                sql += " AND (target_date IS NULL OR target_date >= @start)";
                parameters.Add(new NpgsqlParameter("@start", filter.StartDate.Value));
            }

            if (filter?.EndDate is not null)
            {
                sql += " AND (target_date IS NULL OR target_date <= @end)";
                parameters.Add(new NpgsqlParameter("@end", filter.EndDate.Value));
            }

            sql += " ORDER BY target_date NULLS LAST";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            var results = new List<PlanRecord>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(MapPlan(reader));
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch plan records for vehicle {VehicleId} from Postgres.", vehicleId);
            throw;
        }
    }

    public async Task<int> SavePlanRecordAsync(PlanRecord record)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (record.Id == 0)
            {
                const string insertSql = @"INSERT INTO plan_records
                    (vehicle_id, progress, priority, title, description, is_archived, target_date)
                    VALUES (@vehicle_id, @progress, @priority, @title, @description, @is_archived, @target_date)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyPlanParameters(cmd, record);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                record.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE plan_records
                    SET progress = @progress,
                        priority = @priority,
                        title = @title,
                        description = @description,
                        is_archived = @is_archived,
                        target_date = @target_date
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyPlanParameters(cmd, record);
                cmd.Parameters.AddWithValue("@id", record.Id);

                await cmd.ExecuteNonQueryAsync();
                return record.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save plan record {PlanRecordId} for vehicle {VehicleId} in Postgres.", record.Id, record.VehicleId);
            throw;
        }
    }

    public async Task DeletePlanRecordAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM plan_records WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete plan record {PlanRecordId} from Postgres.", id);
            throw;
        }
    }

    private static PlanRecord MapPlan(NpgsqlDataReader reader)
    {
        var progressText = reader.IsDBNull(reader.GetOrdinal("progress")) ? null : reader.GetString(reader.GetOrdinal("progress"));
        var priorityText = reader.IsDBNull(reader.GetOrdinal("priority")) ? null : reader.GetString(reader.GetOrdinal("priority"));

        System.Enum.TryParse(progressText, true, out PlanProgress progress);
        System.Enum.TryParse(priorityText, true, out PlanPriority priority);

        return new PlanRecord
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            VehicleId = reader.GetInt32(reader.GetOrdinal("vehicle_id")),
            Progress = progress,
            Priority = priority,
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? string.Empty : reader.GetString(reader.GetOrdinal("description")),
            IsArchived = reader.GetBoolean(reader.GetOrdinal("is_archived")),
            PlannedDate = reader.IsDBNull(reader.GetOrdinal("target_date")) ? null : reader.GetDateTime(reader.GetOrdinal("target_date")),
            EstimatedCost = null,
            Type = string.Empty
        };
    }

    private static void ApplyPlanParameters(NpgsqlCommand cmd, PlanRecord record)
    {
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@progress", record.Progress.ToString());
        cmd.Parameters.AddWithValue("@priority", record.Priority.ToString());
        cmd.Parameters.AddWithValue("@title", DBNull.Value);
        cmd.Parameters.AddWithValue("@description", record.Description ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@is_archived", record.IsArchived);
        cmd.Parameters.AddWithValue("@target_date", record.PlannedDate.HasValue ? record.PlannedDate.Value.Date : (object)DBNull.Value);
    }
}
