using Automax.External.Implementations.Postgres;
using Automax.External.Interfaces;
using Automax.Helper;
using Automax.Logic;
using Automax.Models.Settings;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Automax.Models.User;
using Automax.Models.Vehicle;
using Automax.Models.Reminder;
using Automax.Enum;

namespace Automax.Tests;

public class ReminderEmailConfigTests
{
    [PostgresIntegrationFact]
    public async Task ReminderEmailToggle_PersistsAndEnablesDigests()
    {
        var connString = Environment.GetEnvironmentVariable("AUTOMAX_POSTGRES_CONNECTION_STRING")
                         ?? "Host=localhost;Database=automax;Username=automax;Password=pass";

        var configHelper = new ConfigHelper(NullLogger<ConfigHelper>.Instance);
        var backup = configHelper.LoadServerConfig();

        var options = Options.Create(new ServerConfig
        {
            StorageProvider = "Postgres",
            PostgresConnectionString = connString
        });

        var pgFactory = new PostgresConnectionFactory(options, NullLogger<PostgresConnectionFactory>.Instance);
        var vehicleData = new PostgresVehicleDataAccess(pgFactory, NullLogger<PostgresVehicleDataAccess>.Instance);
        var reminderData = new PostgresReminderRecordDataAccess(pgFactory, NullLogger<PostgresReminderRecordDataAccess>.Instance);
        var userAccessData = new PostgresUserAccessDataAccess(pgFactory, NullLogger<PostgresUserAccessDataAccess>.Instance);
        var userData = new PostgresUserRecordDataAccess(pgFactory, NullLogger<PostgresUserRecordDataAccess>.Instance);

        var userLogic = new UserLogic(userAccessData);
        var reminderLogic = new ReminderLogic(userLogic, vehicleData, reminderData);
        var reminderEmailLogic = new ReminderEmailLogic(configHelper, reminderLogic, userData);

        try
        {
            var enabledConfig = CloneConfig(backup);
            enabledConfig.EnableReminderEmails = true;
            enabledConfig.ReminderEmailDaysAhead = 30;
            configHelper.SaveServerConfig(enabledConfig);

            var user = new UserData
            {
                UserName = $"digest_{Guid.NewGuid():N}",
                EmailAddress = "digest@example.com",
                PasswordHash = "hash",
                IsAdmin = true,
                IsRootUser = true
            };
            var userId = await userData.SaveUserAsync(user);

            var vehicle = new Vehicle
            {
                Year = 2022,
                Make = "Test",
                Model = "Reminder",
                LicensePlate = $"REM-{Guid.NewGuid():N}".Substring(0, 8)
            };
            var vehicleId = await vehicleData.SaveVehicleAsync(vehicle);

            var reminder = new ReminderRecord
            {
                VehicleId = vehicleId,
                Description = "Upcoming service",
                Metric = ReminderMetric.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(7),
                Urgency = ReminderUrgency.Urgent,
                IsCompleted = false,
                Tags = "test"
            };
            var reminderId = await reminderData.SaveReminderRecordAsync(reminder);

            var digests = await reminderEmailLogic.BuildReminderEmailDigestsAsync();
            Assert.NotNull(digests);
            Assert.Contains(digests, d => d.EmailAddress == user.EmailAddress);

            await reminderData.DeleteReminderRecordAsync(reminderId);
            await vehicleData.DeleteVehicleAsync(vehicleId);
            await userData.DeleteUserAsync(userId);
        }
        finally
        {
            configHelper.SaveServerConfig(backup);
        }
    }

    private static ServerConfig CloneConfig(ServerConfig source)
    {
        return new ServerConfig
        {
            EnableAuth = source.EnableAuth,
            OpenRegistration = source.OpenRegistration,
            DisableRegistration = source.DisableRegistration,
            EnableRootUserOidc = source.EnableRootUserOidc,
            DefaultReminderEmail = source.DefaultReminderEmail,
            WebHookUrl = source.WebHookUrl,
            CustomLogoUrl = source.CustomLogoUrl,
            AllowedFileExtensions = source.AllowedFileExtensions?.ToList() ?? new List<string>(),
            LocaleOverride = source.LocaleOverride,
            LocaleDateTimeOverride = source.LocaleDateTimeOverride,
            InvariantApiEnabled = source.InvariantApiEnabled,
            CustomWidgetsEnabled = source.CustomWidgetsEnabled,
            ReminderUrgencyConfig = source.ReminderUrgencyConfig,
            Motd = source.Motd,
            MailConfig = source.MailConfig,
            Domain = source.Domain,
            OidcConfig = source.OidcConfig,
            MaxDocumentUploadBytes = source.MaxDocumentUploadBytes,
            EnableReminderEmails = source.EnableReminderEmails,
            ReminderEmailDaysAhead = source.ReminderEmailDaysAhead,
            StorageProvider = source.StorageProvider,
            PostgresConnectionString = source.PostgresConnectionString
        };
    }
}
