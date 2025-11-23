using System.ComponentModel.DataAnnotations;
using CarCareTracker.Enum;
using CarCareTracker.Models.Shared;

namespace CarCareTracker.Models.Reminder;

public class ReminderRecord : IValidatableObject
{
    public int Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    public ReminderMetric Metric { get; set; } = ReminderMetric.Date;

    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime? DueDate { get; set; }
    public int? DueOdometer { get; set; }

    public ReminderUrgency Urgency { get; set; } = ReminderUrgency.NotUrgent;

    public bool IsCompleted { get; set; }
    public DateTime? CompletedDate { get; set; }

    public string? Tags { get; set; }

    public List<ExtraField> ExtraFields { get; set; } = new();
    public UploadedFiles? Files { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Metric == ReminderMetric.Date && !DueDate.HasValue)
        {
            yield return new ValidationResult(
                "Due date is required for date-based reminders.",
                new[] { nameof(DueDate) });
        }
    }
}

public class ReminderRecordInput : IValidatableObject
{
    public int? Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    public ReminderMetric Metric { get; set; } = ReminderMetric.Date;

    [DataType(DataType.Date)]
    [Display(Name = "Due Date")]
    public DateTime? DueDate { get; set; }
    public int? DueOdometer { get; set; }

    public string? Tags { get; set; }

    public List<ExtraField> ExtraFields { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Metric == ReminderMetric.Date && !DueDate.HasValue)
        {
            yield return new ValidationResult(
                "Due date is required for date-based reminders.",
                new[] { nameof(DueDate) });
        }
    }
}

public class ReminderRecordViewModel
{
    public ReminderRecord Reminder { get; set; } = new();
    public ReminderUrgency Urgency => Reminder.Urgency;

    public bool IsPastDue { get; set; }
    public bool IsVeryUrgent { get; set; }
    public bool IsUrgent { get; set; }
}

// TODO: add recurrence (month/mileage interval) fields in a later phase per spec.
