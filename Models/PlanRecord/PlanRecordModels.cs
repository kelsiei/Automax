using System.ComponentModel.DataAnnotations;
using CarCareTracker.Enum;
using CarCareTracker.Models.Shared;

namespace CarCareTracker.Models.PlanRecord;

public class PlanRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    public DateTime? PlannedDate { get; set; }
    [Range(0, 999999999, ErrorMessage = "Estimated cost cannot be negative.")]
    [Display(Name = "Estimated Cost")]
    public decimal? EstimatedCost { get; set; }

    public PlanPriority Priority { get; set; } = PlanPriority.Medium;
    public PlanProgress Progress { get; set; } = PlanProgress.NotStarted;

    // Type: Service, Repair, Upgrade, etc.
    public string Type { get; set; } = string.Empty;

    public List<ExtraField> ExtraFields { get; set; } = new();
    public UploadedFiles? Files { get; set; }

    public bool IsArchived { get; set; }
}

public class PlanRecordInput
{
    public int? Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    public DateTime? PlannedDate { get; set; }
    [Range(0, 999999999, ErrorMessage = "Estimated cost cannot be negative.")]
    [Display(Name = "Estimated Cost")]
    public decimal? EstimatedCost { get; set; }

    public PlanPriority Priority { get; set; } = PlanPriority.Medium;
    public PlanProgress Progress { get; set; } = PlanProgress.NotStarted;

    public string Type { get; set; } = string.Empty;

    public List<ExtraField> ExtraFields { get; set; } = new();
}

public class PlanRecordTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal? EstimatedCost { get; set; }
    public PlanPriority Priority { get; set; } = PlanPriority.Medium;
    public string Type { get; set; } = string.Empty;

    public List<ExtraField> ExtraFields { get; set; } = new();
}

// TODO: Add requisition history and supply integration fields in later phases.
