#nullable enable
using Automax.External.Implementations.Litedb;
using Automax.External.Implementations.Postgres;
using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Automax.Tests;

public class ProgramConfigurationTests
{
    [Fact]
    public void DefaultStorageProvider_RegistersLiteDbImplementations()
    {
        var services = BuildServicesWithConfig(new Dictionary<string, string>());

        Assert.IsType<LiteDbVehicleDataAccess>(services.GetRequiredService<IVehicleDataAccess>());
        Assert.IsType<LiteDbGasRecordDataAccess>(services.GetRequiredService<IGasRecordDataAccess>());
        Assert.IsType<LiteDbOdometerRecordDataAccess>(services.GetRequiredService<IOdometerRecordDataAccess>());
        Assert.IsType<LiteDbServiceRecordDataAccess>(services.GetRequiredService<IServiceRecordDataAccess>());
        Assert.IsType<LiteDbPlanRecordDataAccess>(services.GetRequiredService<IPlanRecordDataAccess>());
        Assert.IsType<LiteDbReminderRecordDataAccess>(services.GetRequiredService<IReminderRecordDataAccess>());
        Assert.IsType<LiteDbNoteDataAccess>(services.GetRequiredService<INoteDataAccess>());
        Assert.IsType<LiteDbDocumentDataAccess>(services.GetRequiredService<IDocumentDataAccess>());
        Assert.IsType<LiteDbExtraFieldDataAccess>(services.GetRequiredService<IExtraFieldDataAccess>());
    }

    [Fact]
    public void StorageProvider_Postgres_RegistersPostgresStubForVehicles()
    {
        var services = BuildServicesWithConfig(new Dictionary<string, string>
        {
            { "ServerConfig:StorageProvider", "Postgres" },
            { "ServerConfig:PostgresConnectionString", "Host=localhost;Database=automax;Username=user;Password=pass" }
        });

        Assert.IsType<PostgresVehicleDataAccess>(services.GetRequiredService<IVehicleDataAccess>());
        Assert.IsType<PostgresGasRecordDataAccess>(services.GetRequiredService<IGasRecordDataAccess>());
        Assert.IsType<PostgresOdometerDataAccess>(services.GetRequiredService<IOdometerRecordDataAccess>());
        Assert.IsType<PostgresServiceRecordDataAccess>(services.GetRequiredService<IServiceRecordDataAccess>());
        Assert.IsType<PostgresPlanRecordDataAccess>(services.GetRequiredService<IPlanRecordDataAccess>());
        Assert.IsType<PostgresReminderRecordDataAccess>(services.GetRequiredService<IReminderRecordDataAccess>());
        Assert.IsType<PostgresNoteDataAccess>(services.GetRequiredService<INoteDataAccess>());
        Assert.IsType<PostgresDocumentDataAccess>(services.GetRequiredService<IDocumentDataAccess>());
        Assert.IsType<PostgresExtraFieldDataAccess>(services.GetRequiredService<IExtraFieldDataAccess>());
        Assert.IsType<PostgresConnectionFactory>(services.GetRequiredService<PostgresConnectionFactory>());
    }

    private static ServiceProvider BuildServicesWithConfig(IDictionary<string, string> configValues)
    {
        var inMemory = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues.ToDictionary(k => k.Key, v => (string?)v.Value))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        services.Configure<ServerConfig>(inMemory.GetSection("ServerConfig"));
        services.AddSingleton<LiteDBHelper>();
        services.AddSingleton<ConfigHelper>();
        services.AddSingleton<FileHelper>();
        services.AddSingleton<ReminderHelper>();
        services.AddSingleton<BackupHelper>();
        services.AddSingleton<IPasswordHelper, PasswordHelper>();
        services.AddSingleton<LocaleHelper>();

        var serverConfig = inMemory.GetSection("ServerConfig").Get<ServerConfig>() ?? new ServerConfig();
        var storageProvider = serverConfig.StorageProvider?.Trim() ?? "LiteDb";

        if (!storageProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IVehicleDataAccess, LiteDbVehicleDataAccess>();
            services.AddScoped<IGasRecordDataAccess, LiteDbGasRecordDataAccess>();
            services.AddScoped<IOdometerRecordDataAccess, LiteDbOdometerRecordDataAccess>();
            services.AddScoped<IServiceRecordDataAccess, LiteDbServiceRecordDataAccess>();
            services.AddScoped<IPlanRecordDataAccess, LiteDbPlanRecordDataAccess>();
            services.AddScoped<IReminderRecordDataAccess, LiteDbReminderRecordDataAccess>();
            services.AddScoped<INoteDataAccess, LiteDbNoteDataAccess>();
            services.AddScoped<IDocumentDataAccess, LiteDbDocumentDataAccess>();
            services.AddScoped<IExtraFieldDataAccess, LiteDbExtraFieldDataAccess>();
        }
        else
        {
            services.AddSingleton<PostgresConnectionFactory>();
            services.AddScoped<IVehicleDataAccess, PostgresVehicleDataAccess>();
            services.AddScoped<IGasRecordDataAccess, PostgresGasRecordDataAccess>();
            services.AddScoped<IOdometerRecordDataAccess, PostgresOdometerDataAccess>();
            services.AddScoped<IServiceRecordDataAccess, PostgresServiceRecordDataAccess>();
            services.AddScoped<IPlanRecordDataAccess, PostgresPlanRecordDataAccess>();
            services.AddScoped<IReminderRecordDataAccess, PostgresReminderRecordDataAccess>();
            services.AddScoped<INoteDataAccess, PostgresNoteDataAccess>();
            services.AddScoped<IDocumentDataAccess, PostgresDocumentDataAccess>();
            services.AddScoped<IExtraFieldDataAccess, PostgresExtraFieldDataAccess>();
        }

        return services.BuildServiceProvider();
    }
}
