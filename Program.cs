



using CarCareTracker.Helper;
using CarCareTracker.Models.Settings;
using CarCareTracker.Middleware;
using CarCareTracker.External.Interfaces;
using CarCareTracker.External.Implementations.Litedb;
using CarCareTracker.Logic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configuration: base appsettings + env-specific.
// TODO: load data/config/userConfig.json and data/config/serverConfig.json (with migration) in later phase.
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.Configure<ServerConfig>(
    builder.Configuration.GetSection("ServerConfig"));

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<ConfigHelper>();
builder.Services.AddScoped<MailHelper>();
builder.Services.AddSingleton<LiteDBHelper>();
builder.Services.AddSingleton<FileHelper>();
builder.Services.AddSingleton<ReminderHelper>();
builder.Services.AddSingleton<BackupHelper>();
builder.Services.AddSingleton<IPasswordHelper, PasswordHelper>();
builder.Services.AddSingleton<LocaleHelper>();

// Data access (LiteDB backend - default for now)
builder.Services.AddScoped<IVehicleDataAccess, LiteDbVehicleDataAccess>();
builder.Services.AddScoped<IGasRecordDataAccess, LiteDbGasRecordDataAccess>();
builder.Services.AddScoped<IServiceRecordDataAccess, LiteDbServiceRecordDataAccess>();
builder.Services.AddScoped<IReminderRecordDataAccess, LiteDbReminderRecordDataAccess>();
builder.Services.AddScoped<IPlanRecordDataAccess, LiteDbPlanRecordDataAccess>();
builder.Services.AddScoped<IOdometerRecordDataAccess, LiteDbOdometerRecordDataAccess>();
builder.Services.AddScoped<INoteDataAccess, LiteDbNoteDataAccess>();
builder.Services.AddScoped<IUserRecordDataAccess, LiteDbUserRecordDataAccess>();
builder.Services.AddScoped<IUserConfigDataAccess, LiteDbUserConfigDataAccess>();
builder.Services.AddScoped<IUserAccessDataAccess, LiteDbUserAccessDataAccess>();
builder.Services.AddScoped<IExtraFieldDataAccess, LiteDbExtraFieldDataAccess>();
// TODO: switch to Postgres implementations when POSTGRES_CONNECTION is provided.

// Logic layer
builder.Services.AddScoped<VehicleLogic>();
builder.Services.AddScoped<UserLogic>();
builder.Services.AddScoped<OdometerLogic>();
builder.Services.AddScoped<ReportLogic>();
builder.Services.AddScoped<ReminderLogic>();
builder.Services.AddScoped<HomeDashboardLogic>();
builder.Services.AddScoped<ReminderEmailLogic>();

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

// TODO: register data access interfaces and implementations (LiteDB/Postgres) in later phase.
// TODO: register helpers (StaticHelper, FileHelper, ConfigHelper, etc.) and logic classes in later phase.
// TODO: configure culture overrides, directory creation, migrations, webhook configuration, etc.

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
    var serverConfig = configHelper.LoadServerConfig();
// app.UseSecurityHeaders();
    var localeHelper = services.GetRequiredService<LocaleHelper>();
    var locOptions = localeHelper.BuildRequestLocalizationOptions(serverConfig);

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
