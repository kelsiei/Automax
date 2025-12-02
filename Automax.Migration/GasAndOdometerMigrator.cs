using System;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.Models.GasRecord;
using Automax.Models.OdometerRecord;
using LiteDB;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.Migration;

internal class GasAndOdometerMigrator
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<GasAndOdometerMigrator> _logger;

    public GasAndOdometerMigrator(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<GasAndOdometerMigrator> logger)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting gas + odometer migration. DryRun={DryRun}", dryRun);

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        var gasCollection = lite.GetCollection<GasRecord>("gas_records");
        var odoCollection = lite.GetCollection<OdometerRecord>("odometer_records");

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var migrated = 0;

        foreach (var gas in gasCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (gas.VehicleId == 0)
            {
                _logger.LogWarning("Skipping gas record {GasId} because VehicleId is 0.", gas.Id);
                continue;
            }

            if (!await VehicleExistsAsync(conn, gas.VehicleId, cancellationToken))
            {
                _logger.LogWarning("Skipping gas record {GasId} for vehicle {VehicleId}: vehicle not found in Postgres.", gas.Id, gas.VehicleId);
                continue;
            }

            if (await GasExistsAsync(conn, gas.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping gas record {GasId} (already exists).", gas.Id);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate gas record {GasId} for vehicle {VehicleId} (Date={Date}, TotalCost={TotalCost}).", gas.Id, gas.VehicleId, gas.Date, gas.TotalCost);
                migrated++;
                continue;
            }

            await InsertGasAsync(conn, gas, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated gas record {GasId} for vehicle {VehicleId}.", gas.Id, gas.VehicleId);
        }

        foreach (var odo in odoCollection.FindAll())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (odo.VehicleId == 0)
            {
                _logger.LogWarning("Skipping odometer record {OdoId} because VehicleId is 0.", odo.Id);
                continue;
            }

            if (!await VehicleExistsAsync(conn, odo.VehicleId, cancellationToken))
            {
                _logger.LogWarning("Skipping odometer record {OdoId} for vehicle {VehicleId}: vehicle not found in Postgres.", odo.Id, odo.VehicleId);
                continue;
            }

            if (await OdometerExistsAsync(conn, odo.Id, cancellationToken))
            {
                _logger.LogInformation("Skipping odometer record {OdoId} (already exists).", odo.Id);
                continue;
            }

            if (dryRun)
            {
                _logger.LogInformation("Would migrate odometer record {OdoId} for vehicle {VehicleId} (Date={Date}, Odometer={Odometer}).", odo.Id, odo.VehicleId, odo.Date, odo.Odometer);
                migrated++;
                continue;
            }

            await InsertOdometerAsync(conn, odo, cancellationToken);
            migrated++;
            _logger.LogInformation("Migrated odometer record {OdoId} for vehicle {VehicleId}.", odo.Id, odo.VehicleId);
        }

        if (!dryRun)
        {
            await SetSequencesAsync(conn, cancellationToken);
        }

        _logger.LogInformation("Gas + odometer migration finished. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
        return migrated;
    }

    private static async Task<bool> VehicleExistsAsync(NpgsqlConnection conn, int vehicleId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM vehicles WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", vehicleId);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task<bool> GasExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM gas_records WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task InsertGasAsync(NpgsqlConnection conn, GasRecord record, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO gas_records
            (id, vehicle_id, date, volume, total_cost, odometer, notes)
            VALUES (@id, @vehicle_id, @date, @volume, @total_cost, @odometer, @notes);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", record.Id);
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@date", record.Date.Date);
        cmd.Parameters.AddWithValue("@volume", record.Volume);
        cmd.Parameters.AddWithValue("@total_cost", record.TotalCost);
        cmd.Parameters.AddWithValue("@odometer", record.Odometer.HasValue ? record.Odometer.Value : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@notes", record.Notes ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> OdometerExistsAsync(NpgsqlConnection conn, int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COUNT(1) FROM odometer_records WHERE id = @id";
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is long l && l > 0 || result is int i && i > 0;
    }

    private static async Task InsertOdometerAsync(NpgsqlConnection conn, OdometerRecord record, CancellationToken cancellationToken)
    {
        const string sql = @"INSERT INTO odometer_records
            (id, vehicle_id, date_recorded, odometer, notes)
            VALUES (@id, @vehicle_id, @date_recorded, @odometer, @notes);";

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", record.Id);
        cmd.Parameters.AddWithValue("@vehicle_id", record.VehicleId);
        cmd.Parameters.AddWithValue("@date_recorded", record.Date.Date);
        cmd.Parameters.AddWithValue("@odometer", record.Odometer);
        cmd.Parameters.AddWithValue("@notes", record.Notes ?? (object)DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SetSequencesAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        const string gasSql = @"SELECT setval(pg_get_serial_sequence('gas_records','id'), (SELECT COALESCE(MAX(id),0) FROM gas_records))";
        const string odoSql = @"SELECT setval(pg_get_serial_sequence('odometer_records','id'), (SELECT COALESCE(MAX(id),0) FROM odometer_records))";

        await using (var cmd = new NpgsqlCommand(gasSql, conn))
        {
            await cmd.ExecuteScalarAsync(cancellationToken);
        }

        await using (var cmd = new NpgsqlCommand(odoSql, conn))
        {
            await cmd.ExecuteScalarAsync(cancellationToken);
        }
    }
}
