using System.Text.Json;
using CarCareTracker.Models.Settings;
using Microsoft.Extensions.Logging;

namespace CarCareTracker.Helper;

public class ConfigHelper
{
    private readonly ILogger<ConfigHelper> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    private string ServerConfigPath => Path.Combine(StaticHelper.ConfigDirectory, "serverConfig.json");
    private string UserConfigPath => Path.Combine(StaticHelper.ConfigDirectory, "userConfig.json");

    public ConfigHelper(ILogger<ConfigHelper> logger)
    {
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    public ServerConfig LoadServerConfig()
    {
        StaticHelper.EnsureDataDirectoriesExist(_logger);
        try
        {
            if (!File.Exists(ServerConfigPath))
            {
                var defaultConfig = CreateDefaultServerConfig();
                SaveServerConfig(defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(ServerConfigPath);
            var config = JsonSerializer.Deserialize<ServerConfig>(json, _serializerOptions);
            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize server config. Using defaults.");
                return CreateDefaultServerConfig();
            }

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading server config. Using defaults.");
            return CreateDefaultServerConfig();
        }
    }

    public void SaveServerConfig(ServerConfig config)
    {
        StaticHelper.EnsureDataDirectoriesExist(_logger);
        try
        {
            var json = JsonSerializer.Serialize(config, _serializerOptions);
            File.WriteAllText(ServerConfigPath, json);
            _logger.LogInformation("Server config saved to {Path}", ServerConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save server config to {Path}", ServerConfigPath);
        }
    }

    public UserConfig LoadUserConfig()
    {
        StaticHelper.EnsureDataDirectoriesExist(_logger);
        try
        {
            if (!File.Exists(UserConfigPath))
            {
                var defaultConfig = CreateDefaultUserConfig();
                SaveUserConfig(defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(UserConfigPath);
            var config = JsonSerializer.Deserialize<UserConfig>(json, _serializerOptions);
            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize user config. Using defaults.");
                return CreateDefaultUserConfig();
            }

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading user config. Using defaults.");
            return CreateDefaultUserConfig();
        }
    }

    public void SaveUserConfig(UserConfig config)
    {
        StaticHelper.EnsureDataDirectoriesExist(_logger);
        try
        {
            var json = JsonSerializer.Serialize(config, _serializerOptions);
            File.WriteAllText(UserConfigPath, json);
            _logger.LogInformation("User config saved to {Path}", UserConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save user config to {Path}", UserConfigPath);
        }
    }

    private static ServerConfig CreateDefaultServerConfig()
    {
        return new ServerConfig
        {
            EnableAuth = true,
            OpenRegistration = false,
            DisableRegistration = false,
            EnableRootUserOidc = false,
            DefaultReminderEmail = string.Empty,
            WebHookUrl = string.Empty,
            CustomLogoUrl = string.Empty,
            AllowedFileExtensions = new List<string>(),
            LocaleOverride = string.Empty,
            LocaleDateTimeOverride = string.Empty,
            InvariantApiEnabled = false,
            CustomWidgetsEnabled = false,
            ReminderUrgencyConfig = new ReminderUrgencyConfig(),
            Motd = "Welcome to CarCareTracker",
            MailConfig = new MailConfig(),
            Domain = string.Empty,
            OidcConfig = null
        };
    }

    private static UserConfig CreateDefaultUserConfig()
    {
        return new UserConfig
        {
            DefaultTab = string.Empty,
            VisibleTabs = new List<string>(),
            HideSoldVehicles = false,
            DistanceUnit = string.Empty,
            FuelEconomyUnit = string.Empty,
            CurrencySymbol = string.Empty,
            ReminderUrgencyConfig = new ReminderUrgencyConfig(),
            ColumnPreferences = new List<UserColumnPreference>(),
            DashboardMetrics = new List<Enum.DashboardMetric>()
        };
    }

    // TODO: add migration logic from legacy config directories and per-user config handling.
}
