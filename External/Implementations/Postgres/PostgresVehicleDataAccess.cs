using System;
using System.Linq;
using Automax.External.Interfaces;
using Automax.Models.Vehicle;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.External.Implementations.Postgres;

/// <summary>
/// Postgres implementation for vehicle reads/writes; other entities remain unimplemented.
/// Refer to:
/// - docs/db/postgres-schema.sql
/// - docs/db/POSTGRES_MIGRATION_NOTES.md
/// - docs/db/POSTGRES_PROVIDER_IMPLEMENTATION_PLAN.md
/// for the planned schema and implementation details.
/// </summary>
public class PostgresVehicleDataAccess : IVehicleDataAccess
{
    private readonly ILogger<PostgresVehicleDataAccess> _logger;
    private readonly PostgresConnectionFactory _connectionFactory;

    public PostgresVehicleDataAccess(PostgresConnectionFactory connectionFactory, ILogger<PostgresVehicleDataAccess> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Vehicle?> GetVehicleAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"SELECT id, year, make, model, license_plate, vin
                                 FROM vehicles
                                 WHERE id = @id";

            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapVehicle(reader);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch vehicle {VehicleId} from Postgres.", id);
            throw;
        }
    }

    public async Task<List<Vehicle>> GetVehiclesAsync(int userId, bool isRootUser, IEnumerable<int>? allowedVehicleIds)
    {
        try
        {
            if (!isRootUser && (allowedVehicleIds == null || !allowedVehicleIds.Any()))
            {
                return new List<Vehicle>();
            }

            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            string sql = @"SELECT id, year, make, model, license_plate, vin
                           FROM vehicles";

            if (!isRootUser)
            {
                sql += " WHERE id = ANY(@ids)";
            }

            sql += " ORDER BY id";

            await using var cmd = new NpgsqlCommand(sql, conn);

            if (!isRootUser)
            {
                var idsArray = allowedVehicleIds!.ToArray();
                cmd.Parameters.AddWithValue("@ids", idsArray);
            }

            var vehicles = new List<Vehicle>();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                vehicles.Add(MapVehicle(reader));
            }

            return vehicles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch vehicles from Postgres.");
            throw;
        }
    }

    public async Task<int> SaveVehicleAsync(Vehicle vehicle)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            if (vehicle.Id == 0)
            {
                const string insertSql = @"INSERT INTO vehicles
                    (year, make, model, license_plate, vin, color, purchase_date, purchase_price, notes)
                    VALUES (@year, @make, @model, @plate, @vin, @color, @purchase_date, @purchase_price, @notes)
                    RETURNING id;";

                await using var cmd = new NpgsqlCommand(insertSql, conn);
                ApplyVehicleParameters(cmd, vehicle);

                var newId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                vehicle.Id = newId;
                return newId;
            }
            else
            {
                const string updateSql = @"UPDATE vehicles
                    SET year = @year,
                        make = @make,
                        model = @model,
                        license_plate = @plate,
                        vin = @vin,
                        color = @color,
                        purchase_date = @purchase_date,
                        purchase_price = @purchase_price,
                        notes = @notes
                    WHERE id = @id;";

                await using var cmd = new NpgsqlCommand(updateSql, conn);
                ApplyVehicleParameters(cmd, vehicle);
                cmd.Parameters.AddWithValue("@id", vehicle.Id);

                await cmd.ExecuteNonQueryAsync();
                return vehicle.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save vehicle {VehicleId} to Postgres.", vehicle.Id);
            throw;
        }
    }

    public async Task DeleteVehicleAsync(int id)
    {
        try
        {
            await using var conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            const string sql = @"DELETE FROM vehicles WHERE id = @id";
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vehicle {VehicleId} from Postgres.", id);
            throw;
        }
    }

    private static Vehicle MapVehicle(NpgsqlDataReader reader)
    {
        return new Vehicle
        {
            Id = reader.GetInt32(reader.GetOrdinal("id")),
            Year = reader.GetInt32(reader.GetOrdinal("year")),
            Make = reader.IsDBNull(reader.GetOrdinal("make")) ? null : reader.GetString(reader.GetOrdinal("make")),
            Model = reader.IsDBNull(reader.GetOrdinal("model")) ? null : reader.GetString(reader.GetOrdinal("model")),
            LicensePlate = reader.IsDBNull(reader.GetOrdinal("license_plate")) ? null : reader.GetString(reader.GetOrdinal("license_plate")),
            VehicleIdentifier = reader.IsDBNull(reader.GetOrdinal("vin")) ? null : reader.GetString(reader.GetOrdinal("vin"))
        };
    }

    private static void ApplyVehicleParameters(NpgsqlCommand cmd, Vehicle vehicle)
    {
        cmd.Parameters.AddWithValue("@year", vehicle.Year);
        cmd.Parameters.AddWithValue("@make", vehicle.Make ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@model", vehicle.Model ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@plate", vehicle.LicensePlate ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@vin", vehicle.VehicleIdentifier ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@color", DBNull.Value);
        cmd.Parameters.AddWithValue("@purchase_date", DBNull.Value);
        cmd.Parameters.AddWithValue("@purchase_price", DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", DBNull.Value);
    }
}
