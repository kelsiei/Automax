using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Npgsql;
using Automax.External.Implementations.Postgres;

namespace Automax.Migration;

internal class MigrationRelationsVerifier
{
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<MigrationRelationsVerifier> _logger;

    public MigrationRelationsVerifier(PostgresConnectionFactory connectionFactory, ILogger<MigrationRelationsVerifier> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<int> VerifyAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting relational integrity verification in Postgres.");

        await using var conn = _connectionFactory.CreateConnection();
        await conn.OpenAsync(cancellationToken);

        var mismatches = 0;

        mismatches += await VerifyRelationAsync("gas_records → vehicles", @"SELECT COUNT(*) FROM gas_records g LEFT JOIN vehicles v ON g.vehicle_id = v.id WHERE v.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("odometer_records → vehicles", @"SELECT COUNT(*) FROM odometer_records o LEFT JOIN vehicles v ON o.vehicle_id = v.id WHERE v.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("service_records → vehicles", @"SELECT COUNT(*) FROM service_records s LEFT JOIN vehicles v ON s.vehicle_id = v.id WHERE v.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("plan_records → vehicles", @"SELECT COUNT(*) FROM plan_records p LEFT JOIN vehicles v ON p.vehicle_id = v.id WHERE v.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("reminder_records → vehicles", @"SELECT COUNT(*) FROM reminder_records r LEFT JOIN vehicles v ON r.vehicle_id = v.id WHERE v.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("notes → vehicles", @"SELECT COUNT(*) FROM notes n LEFT JOIN vehicles v ON n.vehicle_id = v.id WHERE v.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("documents → vehicles", @"SELECT COUNT(*) FROM documents d LEFT JOIN vehicles v ON d.vehicle_id = v.id WHERE v.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("user_access.user_id → users", @"SELECT COUNT(*) FROM user_access a LEFT JOIN users u ON a.user_id = u.id WHERE u.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("user_access.vehicle_id → vehicles", @"SELECT COUNT(*) FROM user_access a LEFT JOIN vehicles v ON a.vehicle_id = v.id WHERE v.id IS NULL;", conn, cancellationToken);
        mismatches += await VerifyRelationAsync("user_configs.user_id → users", @"SELECT COUNT(*) FROM user_configs c LEFT JOIN users u ON c.user_id = u.id WHERE u.id IS NULL;", conn, cancellationToken);
        // record_extra_fields is polymorphic; parent validation is not performed here.

        if (mismatches == 0)
        {
            _logger.LogInformation("Relational verification finished. No orphaned rows detected.");
        }
        else
        {
            _logger.LogWarning("Relational verification finished with mismatches. RelationsWithOrphans={Mismatches}", mismatches);
        }

        return mismatches;
    }

    private async Task<int> VerifyRelationAsync(string relationName, string sql, NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        var orphans = result switch
        {
            long l => l,
            int i => i,
            decimal d => (long)d,
            _ => 0L
        };

        if (orphans == 0)
        {
            _logger.LogInformation("Relation {Relation}: OK (no orphans).", relationName);
            return 0;
        }

        _logger.LogWarning("Relation {Relation}: {Orphans} orphan rows detected.", relationName, orphans);
        return 1;
    }
}
