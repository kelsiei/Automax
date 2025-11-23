using System.ComponentModel.DataAnnotations;

namespace CarCareTracker.Models.Settings;

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

    [Display(Name = "Enable reminder email digests")]
    public bool EnableReminderEmails { get; set; }

    [Display(Name = "Reminder email window (days ahead)")]
    [Range(1, 365, ErrorMessage = "Please enter a value between 1 and 365.")]
    public int? ReminderEmailDaysAhead { get; set; }
}
