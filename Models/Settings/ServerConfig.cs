using CarCareTracker.Enum;

namespace CarCareTracker.Models.Settings;

public class ServerConfig
{
    public bool EnableAuth { get; set; }
    public bool OpenRegistration { get; set; }
    public bool DisableRegistration { get; set; }
    public bool EnableRootUserOidc { get; set; }
    public string? DefaultReminderEmail { get; set; }
    public string? WebHookUrl { get; set; }
    public string? CustomLogoUrl { get; set; }
    public List<string> AllowedFileExtensions { get; set; } = new();
    public string? LocaleOverride { get; set; }
    public string? LocaleDateTimeOverride { get; set; }
    public bool InvariantApiEnabled { get; set; }
    public bool CustomWidgetsEnabled { get; set; }
    public ReminderUrgencyConfig? ReminderUrgencyConfig { get; set; }
    public string? Motd { get; set; }
    public MailConfig? MailConfig { get; set; }
    public string? Domain { get; set; }
    public object? OidcConfig { get; set; }
    public long? MaxDocumentUploadBytes { get; set; }
    public bool EnableReminderEmails { get; set; }
    public int? ReminderEmailDaysAhead { get; set; }
    // TODO: add fields for additional server settings (logos, tabs visibility, etc.) as needed.
}
