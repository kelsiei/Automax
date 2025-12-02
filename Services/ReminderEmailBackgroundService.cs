#nullable enable
using Automax.Helper;
using Automax.Logic;
using Automax.Models.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Automax.Services;

/// <summary>
/// Periodically triggers reminder email digests based on server configuration.
/// </summary>
public class ReminderEmailBackgroundService : BackgroundService
{
    private readonly ILogger<ReminderEmailBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConfigHelper _configHelper;
    private readonly TimeSpan _defaultInterval = TimeSpan.FromHours(24);

    public ReminderEmailBackgroundService(
        ILogger<ReminderEmailBackgroundService> logger,
        IServiceScopeFactory scopeFactory,
        ConfigHelper configHelper)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configHelper = configHelper;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var serverConfig = _configHelper.LoadServerConfig();
                if (!serverConfig.EnableReminderEmails)
                {
                    _logger.LogDebug("Reminder email scheduler is disabled; skipping run.");
                }
                else
                {
                    _logger.LogInformation("Reminder email scheduler executing.");
                    using var scope = _scopeFactory.CreateScope();
                    var reminderEmailLogic = scope.ServiceProvider.GetRequiredService<ReminderEmailLogic>();
                    await reminderEmailLogic.BuildReminderEmailDigestsAsync();
                }

                var interval = GetInterval(serverConfig);
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Shutdown requested; exit loop.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reminder email scheduler run.");
                // Small backoff to avoid tight error loops.
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private TimeSpan GetInterval(ServerConfig serverConfig)
    {
        if (serverConfig.ReminderEmailDaysAhead.HasValue && serverConfig.ReminderEmailDaysAhead.Value > 0)
        {
            var minutes = serverConfig.ReminderEmailDaysAhead.Value * 60 * 24;
            return TimeSpan.FromMinutes(minutes);
        }

        return _defaultInterval;
    }
}
