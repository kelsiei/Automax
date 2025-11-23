using System.Net;
using System.Net.Mail;
using CarCareTracker.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarCareTracker.Helper;

public class MailHelper
{
    private readonly ILogger<MailHelper> _logger;
    private readonly ServerConfig _serverConfig;

    public MailHelper(ILogger<MailHelper> logger, IOptions<ServerConfig> serverConfigOptions)
    {
        _logger = logger;
        _serverConfig = serverConfigOptions.Value ?? new ServerConfig();
    }

    private MailConfig? GetMailConfig()
    {
        var config = _serverConfig.MailConfig;
        if (config == null || string.IsNullOrWhiteSpace(config.Host))
        {
            _logger.LogWarning("MailConfig is missing or incomplete. Email will not be sent.");
            return null;
        }

        return config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var config = GetMailConfig();
        if (config == null)
        {
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                using var client = new SmtpClient(config.Host!, config.Port)
                {
                    EnableSsl = config.UseSsl
                };

                if (!string.IsNullOrWhiteSpace(config.UserName) && !string.IsNullOrWhiteSpace(config.Password))
                {
                    client.Credentials = new NetworkCredential(config.UserName, config.Password);
                }

                var fromEmail = string.IsNullOrWhiteSpace(config.FromEmail) ? "no-reply@example.com" : config.FromEmail;
                var fromName = string.IsNullOrWhiteSpace(config.FromName) ? "CarCareTracker" : config.FromName;

                using var message = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true // TODO: make HTML vs plain text configurable.
                };

                message.To.Add(toEmail);
                client.Send(message);
                _logger.LogInformation("Email sent to {Recipient} with subject {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {Recipient}", toEmail);
            }
        }, cancellationToken);
    }

    public Task SendTestEmailAsync(string toEmail, CancellationToken cancellationToken = default)
    {
        const string subject = "CarCareTracker test email";
        const string body = "This is a test email from CarCareTracker.";
        return SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    public Task SendReminderDigestEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        return SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    // TODO: integrate with reminder notifications and Home/SendTestEmail endpoint in later phases.
}
