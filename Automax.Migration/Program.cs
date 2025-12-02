using Automax.External.Implementations.Postgres;
using Automax.External.Interfaces;
using Automax.Models.Settings;
using Automax.Models.User;
using Automax.Models.Vehicle;
using Automax.Migration;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var exitCode = await RunAsync(args);
return exitCode;

static async Task<int> RunAsync(string[] args)
{
    var (litePath, pgConnection, mode, apply, sourceRoot, targetRoot) = ParseArgs(args);
    var dryRun = !apply;

    if (string.IsNullOrWhiteSpace(litePath))
    {
        PrintUsage();
        return 1;
    }

    var modeRequiresPostgres = !mode.Equals("inspect", StringComparison.OrdinalIgnoreCase) &&
                               !mode.Equals("migrate-document-files", StringComparison.OrdinalIgnoreCase);

    var services = new ServiceCollection();
    services.AddLogging(builder => builder.AddSimpleConsole(options => options.SingleLine = true));

    if (!string.IsNullOrWhiteSpace(pgConnection))
    {
        services.Configure<ServerConfig>(opts =>
        {
            opts.StorageProvider = "Postgres";
            opts.PostgresConnectionString = pgConnection;
        });
        services.AddSingleton<PostgresConnectionFactory>();
        services.AddScoped<IVehicleDataAccess, PostgresVehicleDataAccess>();
        services.AddScoped<IUserRecordDataAccess, PostgresUserRecordDataAccess>();
        services.AddScoped<IGasRecordDataAccess, PostgresGasRecordDataAccess>();
        services.AddScoped<IOdometerRecordDataAccess, PostgresOdometerDataAccess>();
    }

    await LiteDbInspector.InspectAsync(litePath);

    if (!string.IsNullOrWhiteSpace(pgConnection))
    {
        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("PostgresInspector");
        try
        {
            await PostgresInspector.CheckAsync(provider.GetRequiredService<PostgresConnectionFactory>(), logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Postgres connectivity check failed.");
            return 2;
        }

        if (mode.Equals("migrate-vehicles", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<VehiclesMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new VehiclesMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Vehicles migration completed. Migrated {Migrated} vehicles. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Vehicles migration failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-gas-odometer", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<GasAndOdometerMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new GasAndOdometerMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Gas/odometer migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Gas/odometer migration failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-service-reminders", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<ServiceAndReminderMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new ServiceAndReminderMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Service/reminder migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Service/reminder migration failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-plans-notes", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<PlansAndNotesMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new PlansAndNotesMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Plans/notes migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Plans/notes migration failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-users-access-configs", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<UsersAccessConfigsMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new UsersAccessConfigsMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Users/access/configs migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Users/access/configs migration failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-documents", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<DocumentsMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new DocumentsMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Documents migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Documents migration failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-extra-fields", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<ExtraFieldsMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new ExtraFieldsMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Extra fields migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Extra fields migration failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-documents", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<DocumentsMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new DocumentsMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Documents migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Documents migration failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-extra-fields", StringComparison.OrdinalIgnoreCase))
        {
            var migratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<ExtraFieldsMigrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var migrator = new ExtraFieldsMigrator(litePath, factory, migratorLogger);

            try
            {
                var migrated = await migrator.MigrateAsync(dryRun);
                migratorLogger.LogInformation("Extra fields migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                migratorLogger.LogError(ex, "Extra fields migration failed.");
                return 4;
            }
        }

        if (mode.Equals("verify-postgres", StringComparison.OrdinalIgnoreCase))
        {
            var verifierLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<MigrationVerifier>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var verifier = new MigrationVerifier(litePath, factory, verifierLogger);

            try
            {
                var mismatches = await verifier.VerifyAsync();
                if (mismatches == 0)
                {
                    verifierLogger.LogInformation("Verification completed. All entity counts match between LiteDB and Postgres.");
                    return 0;
                }

                verifierLogger.LogWarning("Verification completed with mismatches. MismatchedEntities={Mismatches}", mismatches);
                return 5;
            }
            catch (Exception ex)
            {
                verifierLogger.LogError(ex, "Verification failed.");
                return 4;
            }
        }

        if (mode.Equals("verify-postgres-relations", StringComparison.OrdinalIgnoreCase))
        {
            var verifierLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<MigrationRelationsVerifier>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var verifier = new MigrationRelationsVerifier(factory, verifierLogger);

            try
            {
                var mismatches = await verifier.VerifyAsync();
                if (mismatches == 0)
                {
                    verifierLogger.LogInformation("Relational verification completed. No orphaned rows detected.");
                    return 0;
                }

                verifierLogger.LogWarning("Relational verification completed with mismatches. RelationsWithOrphans={Mismatches}", mismatches);
                return 6;
            }
            catch (Exception ex)
            {
                verifierLogger.LogError(ex, "Relational verification failed.");
                return 4;
            }
        }

        if (mode.Equals("verify-postgres-relations", StringComparison.OrdinalIgnoreCase))
        {
            var verifierLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<MigrationRelationsVerifier>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var verifier = new MigrationRelationsVerifier(factory, verifierLogger);

            try
            {
                var mismatches = await verifier.VerifyAsync();
                if (mismatches == 0)
                {
                    verifierLogger.LogInformation("Relational verification completed. No orphaned rows detected.");
                    return 0;
                }

                verifierLogger.LogWarning("Relational verification completed with mismatches. RelationsWithOrphans={Mismatches}", mismatches);
                return 6;
            }
            catch (Exception ex)
            {
                verifierLogger.LogError(ex, "Relational verification failed.");
                return 4;
            }
        }

        if (mode.Equals("migrate-all", StringComparison.OrdinalIgnoreCase))
        {
            var orchestratorLogger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<FullMigrationOrchestrator>();
            var factory = provider.GetRequiredService<PostgresConnectionFactory>();
            var orchestrator = new FullMigrationOrchestrator(litePath, factory, orchestratorLogger, provider.GetRequiredService<ILoggerFactory>());

            try
            {
                var migrated = await orchestrator.RunAsync(dryRun);
                orchestratorLogger.LogInformation("Full migration completed. Migrated {Migrated} records. DryRun={DryRun}", migrated, dryRun);
                return 0;
            }
            catch (Exception ex)
            {
                orchestratorLogger.LogError(ex, "Full migration failed.");
                return 4;
            }
        }
    }
    else if (modeRequiresPostgres)
    {
        Console.WriteLine("Migration mode requires a Postgres connection string.");
        return 3;
    }
    else
    {
        Console.WriteLine("Postgres connection string not provided. Skipping Postgres connectivity check.");
    }

    if (mode.Equals("migrate-document-files", StringComparison.OrdinalIgnoreCase))
    {
        if (string.IsNullOrWhiteSpace(sourceRoot) || string.IsNullOrWhiteSpace(targetRoot))
        {
            Console.WriteLine("Document files mode requires both source and target roots (via --source-root/--target-root or env vars).");
            return 3;
        }

        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DocumentFilesMigrator>();
        var migrator = new DocumentFilesMigrator(litePath, sourceRoot, targetRoot, logger);

        try
        {
            var migrated = await migrator.MigrateAsync(dryRun);
            logger.LogInformation("Document file migration completed. FilesProcessed={Migrated}. DryRun={DryRun}", migrated, dryRun);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Document file migration failed.");
            return 4;
        }
    }

    return 0;
}

static (string? liteDbPath, string? pgConnectionString, string mode, bool apply, string? sourceRoot, string? targetRoot) ParseArgs(string[] args)
{
    string? litePath = null;
    string? pg = null;
    string mode = "inspect";
    var apply = false;
    string? sourceRoot = null;
    string? targetRoot = null;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--lite":
            case "--litedb":
                if (i + 1 < args.Length)
                {
                    litePath = args[i + 1];
                    i++;
                }
                break;
            case "--pg":
            case "--postgres":
                if (i + 1 < args.Length)
                {
                    pg = args[i + 1];
                    i++;
                }
                break;
            case "--mode":
                if (i + 1 < args.Length)
                {
                    mode = args[i + 1];
                    i++;
                }
                break;
            case "--apply":
                apply = true;
                break;
            case "--source-root":
                if (i + 1 < args.Length)
                {
                    sourceRoot = args[i + 1];
                    i++;
                }
                break;
            case "--target-root":
                if (i + 1 < args.Length)
                {
                    targetRoot = args[i + 1];
                    i++;
                }
                break;
        }
    }

    litePath ??= Environment.GetEnvironmentVariable("AUTOMAX_MIGRATION_LITEDB_PATH");
    pg ??= Environment.GetEnvironmentVariable("AUTOMAX_POSTGRES_CONNECTION_STRING");
    sourceRoot ??= Environment.GetEnvironmentVariable("AUTOMAX_MIGRATION_SOURCE_ROOT");
    targetRoot ??= Environment.GetEnvironmentVariable("AUTOMAX_MIGRATION_TARGET_ROOT");

    return (litePath, pg, mode, apply, sourceRoot, targetRoot);
}

static void PrintUsage()
{
    Console.WriteLine("Automax.Migration - LiteDB -> Postgres migration skeleton");
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project Automax.Migration -- --lite <path-to-lite-db> [--pg <postgres-connection-string>] [--mode inspect|migrate-vehicles|migrate-gas-odometer|migrate-service-reminders|migrate-plans-notes|migrate-users-access-configs|migrate-documents|migrate-extra-fields|migrate-all|verify-postgres|verify-postgres-relations] [--apply]");
    Console.WriteLine("  dotnet run --project Automax.Migration -- --lite <path-to-lite-db> [--pg <postgres-connection-string>] [--mode inspect|migrate-vehicles|migrate-gas-odometer|migrate-service-reminders|migrate-plans-notes|migrate-users-access-configs|migrate-documents|migrate-extra-fields|migrate-all|verify-postgres|verify-postgres-relations|migrate-document-files] [--apply] [--source-root <path>] [--target-root <path>]");
    Console.WriteLine("Environment variables:");
    Console.WriteLine("  AUTOMAX_MIGRATION_LITEDB_PATH: LiteDB database file path");
    Console.WriteLine("  AUTOMAX_POSTGRES_CONNECTION_STRING: Postgres connection string (required for Postgres-backed modes).");
    Console.WriteLine("  AUTOMAX_MIGRATION_SOURCE_ROOT: Source root for document file migration (used by migrate-document-files)");
    Console.WriteLine("  AUTOMAX_MIGRATION_TARGET_ROOT: Target root for document file migration (used by migrate-document-files)");
    Console.WriteLine("Modes:");
    Console.WriteLine("  inspect (default): print LiteDB collection counts and optional Postgres connectivity check.");
    Console.WriteLine("  migrate-vehicles: migrate vehicles from LiteDB to Postgres. Default is dry run; add --apply to write.");
    Console.WriteLine("  migrate-gas-odometer: migrate gas and odometer records from LiteDB to Postgres (requires vehicles present). Default is dry run; add --apply to write.");
    Console.WriteLine("  migrate-service-reminders: migrate service and reminder records from LiteDB to Postgres (requires vehicles present). Default is dry run; add --apply to write.");
    Console.WriteLine("  migrate-plans-notes: migrate plan and note records from LiteDB to Postgres (requires vehicles present). Default is dry run; add --apply to write.");
    Console.WriteLine("  migrate-users-access-configs: migrate users, user access, and user configs from LiteDB to Postgres (requires vehicles present for access). Default is dry run; add --apply to write.");
    Console.WriteLine("  migrate-documents: migrate document metadata from LiteDB to Postgres (requires vehicles present). Default is dry run; add --apply to write.");
    Console.WriteLine("  migrate-extra-fields: migrate extra field records from LiteDB to Postgres. Default is dry run; add --apply to write.");
    Console.WriteLine("  migrate-all: run all migrations in order (vehicles → gas/odometer → service/reminders → plans/notes → users/access/configs → documents → extra fields). Default is dry run; add --apply to write.");
    Console.WriteLine("  verify-postgres: read-only comparison of LiteDB vs Postgres row counts for all entities. Returns non-zero exit code when mismatches are found.");
    Console.WriteLine("  verify-postgres-relations: read-only relational integrity check in Postgres; reports orphaned rows for key relationships and returns non-zero exit code when any are found.");
    Console.WriteLine("  migrate-document-files: optional document file copy based on LiteDB metadata; defaults to dry run. Provide source/target roots via args or env.");
}

internal static class LiteDbInspector
{
    private static readonly string[] Collections =
    {
        "vehicles",
        "gas_records",
        "odometer_records",
        "service_records",
        "plan_records",
        "reminder_records",
        "notes",
        "users",
        "user_access",
        "user_configs"
    };

    public static Task InspectAsync(string liteDbPath)
    {
        Console.WriteLine($"Opening LiteDB at: {liteDbPath}");
        using var db = new LiteDatabase($"Filename={liteDbPath};ReadOnly=true");

        foreach (var name in Collections)
        {
            var count = db.GetCollection(name).Count();
            Console.WriteLine($"  {name}: {count} documents");
        }

        return Task.CompletedTask;
    }
}

internal static class PostgresInspector
{
    public static async Task CheckAsync(PostgresConnectionFactory factory, ILogger logger)
    {
        logger.LogInformation("Checking Postgres connectivity...");
        await using var conn = factory.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new Npgsql.NpgsqlCommand("SELECT 1", conn);
        var result = await cmd.ExecuteScalarAsync();
        logger.LogInformation("Postgres connectivity OK (SELECT 1 returned {Result}).", result);
    }
}
