using System;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.Models.Vehicle;
using LiteDB;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.Migration;

internal class VehiclesMigrator
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<VehiclesMigrator> _logger;

    public VehiclesMigrator(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<VehiclesMigrator> logger)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting vehicles migration. DryRun={DryRun}", dryRun);

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        var vehicles = lite.GetCollection<Vehicle>("vehicles").FindAll();

        var migrated = 0;
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        foreach (var vehicle in vehicles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await VehicleExistsAsync(conn, vehicle.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping vehicle {VehicleId} (already exists).", vehicle.Id);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate vehicle {VehicleId} ({Make} {Model}).", vehicle.Id, vehicle.Make, vehicle.Model);
                migrated++;
                continue;
            }

            await InsertVehicleAsync(conn, vehicle, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated vehicle {VehicleId} ({Make} {Model}).", vehicle.Id, vehicle.Make, vehicle.Model);
        }

        if (!dryRun)
        {
            await SetSequenceAsync(conn, cancellationToken);
        }

        _logger.LogInformation("Vehicles migration finished. Migrated {Migrated} vehicles. DryRun={DryRun}", migrated, dryRun);
        return migrated;
    }

    private static async Task<bool> VehicleExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM vehicles WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task InsertVehicleAsync(NpgsqlConnection conn, Vehicle vehicle, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO vehicles
            (id, year, make, model, license_plate, vin, color, purchase_date, purchase_price, notes)
            VALUES (@id, @year, @make, @model, @license_plate, @vin, @color, @purchase_date, @purchase_price, @notes);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", vehicle.Id);
        cmd.Parameters.AddWithValue("@year", vehicle.Year);
        cmd.Parameters.AddWithValue("@make", string.IsNullOrWhiteSpace(vehicle.Make) ? (object)DBNull.Value : vehicle.Make);
        cmd.Parameters.AddWithValue("@model", string.IsNullOrWhiteSpace(vehicle.Model) ? (object)DBNull.Value : vehicle.Model);
        cmd.Parameters.AddWithValue("@license_plate", string.IsNullOrWhiteSpace(vehicle.LicensePlate) ? (object)DBNull.Value : vehicle.LicensePlate);
        // VehicleIdentifier maps best to VIN column; other fields not present in the model are set to null.
        cmd.Parameters.AddWithValue("@vin", string.IsNullOrWhiteSpace(vehicle.VehicleIdentifier) ? (object)DBNull.Value : vehicle.VehicleIdentifier);
        cmd.Parameters.AddWithValue("@color", DBNull.Value);
        cmd.Parameters.AddWithValue("@purchase_date", DBNull.Value);
        cmd.Parameters.AddWithValue("@purchase_price", DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SetSequenceAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        const string sql = @"SELECT setval(pg_get_serial_sequence('vehicles','id'), (SELECT COALESCE(MAX(id),0) FROM vehicles))";
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteScalarAsync(cancellationToken);
    }
}
