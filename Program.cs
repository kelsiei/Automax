using Automax.Helper;
using Automax.Models.Settings;
using Automax.Middleware;
using Automax.External.Interfaces;
using Automax.External.Implementations.Litedb;
using Automax.External.Implementations.Postgres;
using Automax.Logic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Configuration: base appsettings + env-specific.
// TODO: load data/config/userConfig.json and data/config/serverConfig.json (with migration) in later phase.
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<ConfigHelper>();
builder.Services.AddScoped<MailHelper>();
builder.Services.AddSingleton<LiteDBHelper>();
builder.Services.AddSingleton<FileHelper>();
builder.Services.AddSingleton<ReminderHelper>();
builder.Services.AddSingleton<BackupHelper>();
builder.Services.AddSingleton<IPasswordHelper, PasswordHelper>();
builder.Services.AddSingleton<LocaleHelper>();

var envPostgres = Environment.GetEnvironmentVariable("AUTOMAX_POSTGRES_CONNECTION_STRING");
builder.Services.Configure<ServerConfig>(options =>
{
    builder.Configuration.GetSection("ServerConfig").Bind(options);
    if (!string.IsNullOrWhiteSpace(envPostgres))
    {
        options.PostgresConnectionString = envPostgres;
    }

    if (builder.Environment.IsProduction())
    {
        options.StorageProvider = "Postgres";
    }
    else
    {
        options.StorageProvider ??= "LiteDb";
    }
});

// Data access (Postgres preferred; LiteDB allowed for development)
var serverConfig = builder.Configuration.GetSection("ServerConfig").Get<ServerConfig>() ?? new ServerConfig();
if (!string.IsNullOrWhiteSpace(envPostgres))
{
    serverConfig.PostgresConnectionString = envPostgres;
}
serverConfig.StorageProvider = builder.Environment.IsProduction()
    ? "Postgres"
    : serverConfig.StorageProvider?.Trim() ?? "LiteDb";

var storageProvider = serverConfig.StorageProvider ?? "LiteDb";

if (storageProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase)
    && string.IsNullOrWhiteSpace(serverConfig.PostgresConnectionString))
{
    throw new InvalidOperationException("Postgres storage is selected but no connection string is configured. Set AUTOMAX_POSTGRES_CONNECTION_STRING or ServerConfig:PostgresConnectionString.");
}

if (!storageProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IVehicleDataAccess, LiteDbVehicleDataAccess>();
    builder.Services.AddScoped<IGasRecordDataAccess, LiteDbGasRecordDataAccess>();
    builder.Services.AddScoped<IServiceRecordDataAccess, LiteDbServiceRecordDataAccess>();
    builder.Services.AddScoped<IReminderRecordDataAccess, LiteDbReminderRecordDataAccess>();
    builder.Services.AddScoped<IPlanRecordDataAccess, LiteDbPlanRecordDataAccess>();
    builder.Services.AddScoped<IOdometerRecordDataAccess, LiteDbOdometerRecordDataAccess>();
    builder.Services.AddScoped<INoteDataAccess, LiteDbNoteDataAccess>();
    builder.Services.AddScoped<IDocumentDataAccess, LiteDbDocumentDataAccess>();
    builder.Services.AddScoped<IUserRecordDataAccess, LiteDbUserRecordDataAccess>();
    builder.Services.AddScoped<IUserConfigDataAccess, LiteDbUserConfigDataAccess>();
    builder.Services.AddScoped<IUserAccessDataAccess, LiteDbUserAccessDataAccess>();
    builder.Services.AddScoped<IExtraFieldDataAccess, LiteDbExtraFieldDataAccess>();
}
else
{
    // Postgres provider is the production path; vehicles/gas/odometer/service/plan/reminders/notes/users/user configs/access are implemented, others remain on LiteDB.
    builder.Services.AddSingleton<PostgresConnectionFactory>();
    builder.Services.AddScoped<IVehicleDataAccess, PostgresVehicleDataAccess>();
    builder.Services.AddScoped<IGasRecordDataAccess, PostgresGasRecordDataAccess>();
    builder.Services.AddScoped<IServiceRecordDataAccess, PostgresServiceRecordDataAccess>();
    builder.Services.AddScoped<IReminderRecordDataAccess, PostgresReminderRecordDataAccess>();
    builder.Services.AddScoped<IPlanRecordDataAccess, PostgresPlanRecordDataAccess>();
    builder.Services.AddScoped<IOdometerRecordDataAccess, PostgresOdometerDataAccess>();
    builder.Services.AddScoped<INoteDataAccess, PostgresNoteDataAccess>();
    builder.Services.AddScoped<IDocumentDataAccess, PostgresDocumentDataAccess>();
    builder.Services.AddScoped<IUserRecordDataAccess, PostgresUserRecordDataAccess>();
    builder.Services.AddScoped<IUserConfigDataAccess, PostgresUserConfigDataAccess>();
    builder.Services.AddScoped<IUserAccessDataAccess, PostgresUserAccessDataAccess>();
    builder.Services.AddScoped<IExtraFieldDataAccess, PostgresExtraFieldDataAccess>();
}

// Logic layer
builder.Services.AddScoped<VehicleLogic>();
builder.Services.AddScoped<UserLogic>();
builder.Services.AddScoped<OdometerLogic>();
builder.Services.AddScoped<ReportLogic>();
builder.Services.AddScoped<ReminderLogic>();
builder.Services.AddScoped<HomeDashboardLogic>();
builder.Services.AddScoped<ReminderEmailLogic>();
//builder.Services.AddHostedService<Automax.Services.ReminderEmailBackgroundService>();

builder.Services
    .AddAuthentication("AuthN")
    .AddScheme<AuthenticationSchemeOptions, Authen>("AuthN", options => { });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes("AuthN")
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var loggerFactory = services.GetRequiredService<ILoggerFactory>();
    var startupLogger = loggerFactory.CreateLogger("Startup");
    StaticHelper.EnsureDataDirectoriesExist(startupLogger);

    var configHelper = services.GetRequiredService<ConfigHelper>();
    var loadedServerConfig = configHelper.LoadServerConfig();

    var localeHelper = services.GetRequiredService<LocaleHelper>();
    var locOptions = localeHelper.BuildRequestLocalizationOptions(loadedServerConfig);

    app.UseRequestLocalization(locOptions);
}

app.UseSecurityHeaders();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
