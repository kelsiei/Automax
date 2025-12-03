using System.ComponentModel.DataAnnotations;

namespace Automax.Models.Settings;

public class ServerSettingsViewModel
{
    [Display(Name = "Message of the Day")]
    [MaxLength(500)]
    public string Motd { get; set; } = string.Empty;

    [Display(Name = "Require Login (Enable Authentication)")]
    public bool EnableAuth { get; set; }

    [Display(Name = "Locale (e.g., en-CA, en-US)")]
    [MaxLength(32)]
    public string LocaleOverride { get; set; } = string.Empty;

    [Display(Name = "Date/Time Format Hint (optional)")]
    [MaxLength(64)]
    public string LocaleDateTimeOverride { get; set; } = string.Empty;

    [Display(Name = "Max document upload size (MB)")]
    [Range(1, 1024, ErrorMessage = "Please specify a value between 1 and 1024 MB.")]
    public int? MaxDocumentUploadSizeMb { get; set; }

    [Display(Name = "Allow open registration")]
    public bool OpenRegistration { get; set; }

    [Display(Name = "Disable new registrations")]
    public bool DisableRegistration { get; set; }

    [Display(Name = "Enable root user OIDC")]
    public bool EnableRootUserOidc { get; set; }

    [Display(Name = "Default reminder email (from)")]
    [EmailAddress]
    public string DefaultReminderEmail { get; set; } = string.Empty;

    [Display(Name = "Custom logo URL")]
    [MaxLength(200)]
    public string CustomLogoUrl { get; set; } = string.Empty;

    [Display(Name = "Webhook URL")]
    [MaxLength(200)]
    public string WebHookUrl { get; set; } = string.Empty;

    [Display(Name = "Allowed file extensions (comma separated)")]
    [MaxLength(200)]
    public string AllowedFileExtensions { get; set; } = string.Empty;

    [Display(Name = "Invariant API responses")]
    public bool InvariantApiEnabled { get; set; }

    [Display(Name = "Enable custom widgets")]
    public bool CustomWidgetsEnabled { get; set; }

    [Display(Name = "Domain")]
    [MaxLength(200)]
    public string Domain { get; set; } = string.Empty;

    [Display(Name = "Enable reminder email digests")]
    public bool EnableReminderEmails { get; set; }

    [Display(Name = "Reminder email window (days ahead)")]
    [Range(1, 365, ErrorMessage = "Please enter a value between 1 and 365.")]
    public int? ReminderEmailDaysAhead { get; set; }

    // Mail configuration
    [Display(Name = "SMTP host")]
    [MaxLength(200)]
    public string MailHost { get; set; } = string.Empty;

    [Display(Name = "SMTP port")]
    [Range(1, 65535)]
    public int? MailPort { get; set; }

    [Display(Name = "SMTP username")]
    [MaxLength(200)]
    public string MailUserName { get; set; } = string.Empty;

    [Display(Name = "SMTP password")]
    [DataType(DataType.Password)]
    public string MailPassword { get; set; } = string.Empty;

    [Display(Name = "SMTP use SSL")]
    public bool MailUseSsl { get; set; }

    [Display(Name = "From email")]
    [EmailAddress]
    public string MailFromEmail { get; set; } = string.Empty;

    [Display(Name = "From name")]
    [MaxLength(200)]
    public string MailFromName { get; set; } = string.Empty;

    // Reminder urgency
    [Display(Name = "Days until urgent")]
    [Range(0, 365)]
    public int? UrgencyDaysUntilUrgent { get; set; }

    [Display(Name = "Days until very urgent")]
    [Range(0, 365)]
    public int? UrgencyDaysUntilVeryUrgent { get; set; }

    [Display(Name = "Days until past due")]
    [Range(0, 365)]
    public int? UrgencyDaysUntilPastDue { get; set; }
}
