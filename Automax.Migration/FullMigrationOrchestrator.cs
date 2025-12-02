using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Automax.External.Implementations.Postgres;

namespace Automax.Migration;

internal class FullMigrationOrchestrator
{
    private readonly string _liteDbPath;
    private readonly PostgresConnectionFactory _connectionFactory;
    private readonly ILogger<FullMigrationOrchestrator> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public FullMigrationOrchestrator(string liteDbPath, PostgresConnectionFactory connectionFactory, ILogger<FullMigrationOrchestrator> logger, ILoggerFactory loggerFactory)
    {
        _liteDbPath = liteDbPath;
        _connectionFactory = connectionFactory;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<int> RunAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full migration. DryRun={DryRun}", dryRun);

        var totalMigrated = 0;

        totalMigrated += await new VehiclesMigrator(_liteDbPath, _connectionFactory, _loggerFactory.CreateLogger<VehiclesMigrator>())
            .MigrateAsync(dryRun, cancellationToken);

        totalMigrated += await new GasAndOdometerMigrator(_liteDbPath, _connectionFactory, _loggerFactory.CreateLogger<GasAndOdometerMigrator>())
            .MigrateAsync(dryRun, cancellationToken);

        totalMigrated += await new ServiceAndReminderMigrator(_liteDbPath, _connectionFactory, _loggerFactory.CreateLogger<ServiceAndReminderMigrator>())
            .MigrateAsync(dryRun, cancellationToken);

        totalMigrated += await new PlansAndNotesMigrator(_liteDbPath, _connectionFactory, _loggerFactory.CreateLogger<PlansAndNotesMigrator>())
            .MigrateAsync(dryRun, cancellationToken);

        totalMigrated += await new UsersAccessConfigsMigrator(_liteDbPath, _connectionFactory, _loggerFactory.CreateLogger<UsersAccessConfigsMigrator>())
            .MigrateAsync(dryRun, cancellationToken);

        totalMigrated += await new DocumentsMigrator(_liteDbPath, _connectionFactory, _loggerFactory.CreateLogger<DocumentsMigrator>())
            .MigrateAsync(dryRun, cancellationToken);

        totalMigrated += await new ExtraFieldsMigrator(_liteDbPath, _connectionFactory, _loggerFactory.CreateLogger<ExtraFieldsMigrator>())
            .MigrateAsync(dryRun, cancellationToken);

        _logger.LogInformation("Full migration finished. Total migrated={TotalMigrated}. DryRun={DryRun}", totalMigrated, dryRun);
        return totalMigrated;
    }
}
