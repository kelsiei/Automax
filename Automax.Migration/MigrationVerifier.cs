using System.Threading;
using System.Threading.Tasks;
using Automax.External.Implementations.Postgres;
using Automax.Models.Document;
using Automax.Models.GasRecord;
using Automax.Models.Note;
using Automax.Models.OdometerRecord;
using Automax.Models.PlanRecord;
using Automax.Models.Reminder;
using Automax.Models.ServiceRecord;
using Automax.Models.Shared;
using Automax.Models.User;
using Automax.Models.Vehicle;
using LiteDB;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Automax.Migration;

internal class MigrationVerifier
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<MigrationVerifier> _logger;

    public MigrationVerifier(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<MigrationVerifier> logger)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> VerifyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting verification of LiteDB vs Postgres counts.");

        using var lite = new LiteDatabase($"Filename={_liteDbPath};ReadOnly=true");
        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var mismatches = 0;

        mismatches += await VerifyEntityAsync("vehicles", lite.GetCollection<Vehicle>("vehicles").Count(), () => CountAsync(conn, "vehicles", cancellationToken));
        mismatches += await VerifyEntityAsync("gas_records", lite.GetCollection<GasRecord>("gas_records").Count(), () => CountAsync(conn, "gas_records", cancellationToken));
        mismatches += await VerifyEntityAsync("odometer_records", lite.GetCollection<OdometerRecord>("odometer_records").Count(), () => CountAsync(conn, "odometer_records", cancellationToken));
        mismatches += await VerifyEntityAsync("service_records", lite.GetCollection<ServiceRecord>("service_records").Count(), () => CountAsync(conn, "service_records", cancellationToken));
        mismatches += await VerifyEntityAsync("plan_records", lite.GetCollection<PlanRecord>("plan_records").Count(), () => CountAsync(conn, "plan_records", cancellationToken));
        mismatches += await VerifyEntityAsync("reminder_records", lite.GetCollection<ReminderRecord>("reminder_records").Count(), () => CountAsync(conn, "reminder_records", cancellationToken));
        mismatches += await VerifyEntityAsync("notes", lite.GetCollection<Note>("notes").Count(), () => CountAsync(conn, "notes", cancellationToken));
        mismatches += await VerifyEntityAsync("documents", lite.GetCollection<DocumentMetadata>("documents").Count(), () => CountAsync(conn, "documents", cancellationToken));
        mismatches += await VerifyEntityAsync("users", lite.GetCollection<UserData>("users").Count(), () => CountAsync(conn, "users", cancellationToken));
        mismatches += await VerifyEntityAsync("user_access", lite.GetCollection<UserAccess>("user_access").Count(), () => CountAsync(conn, "user_access", cancellationToken));
        mismatches += await VerifyEntityAsync("user_configs", lite.GetCollection<UserConfigData>("user_configs").Count(), () => CountAsync(conn, "user_configs", cancellationToken));
        mismatches += await VerifyEntityAsync("extra_fields", lite.GetCollection<RecordExtraField>("extra_fields").Count(), () => CountAsync(conn, "record_extra_fields", cancellationToken));

        if (mismatches == 0)
        {
            _logger.LogInformation("Verification finished. All entity counts match.");
        }
        else
        {
            _logger.LogWarning("Verification finished with mismatches. MismatchedEntities={Mismatches}", mismatches);
        }

        return mismatches;
    }

    private async Task<int> VerifyEntityAsync(string entityName, int liteCount, Func<Task<long>> postgresCountFunc)
    {
        var pgCount = await postgresCountFunc();
        var delta = liteCount - pgCount;
        _logger.LogInformation("Verify {Entity}: LiteDB={LiteCount}, Postgres={PgCount}, Delta={Delta}", entityName, liteCount, pgCount, delta);
        return delta == 0 ? 0 : 1;
    }

    private static async Task<long> CountAsync(NpgsqlConnection conn, string tableName, CancellationToken cancellationToken)
    {
        var sql = $"SELECT COUNT(1) FROM {tableName}";
        await using var cmd = new NpgsqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);

        return result switch
        {
            long l => l,
            int i => i,
            decimal d => (long)d,
            _ => 0
        };
    }
}
