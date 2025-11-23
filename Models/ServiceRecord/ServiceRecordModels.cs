using System.ComponentModel.DataAnnotations;
using CarCareTracker.Models.Shared;

namespace CarCareTracker.Models.ServiceRecord;

public class ServiceRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Service Date")]
    public DateTime Date { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Odometer must be zero or greater.")]
    [Display(Name = "Odometer Reading")]
    public int? Odometer { get; set; }

    [Range(0, 999999999, ErrorMessage = "Cost cannot be negative.")]
    [Display(Name = "Service Cost")]
    public decimal? Cost { get; set; }

    public string? Vendor { get; set; }

    public List<ExtraField> ExtraFields { get; set; } = new();
    public UploadedFiles? Files { get; set; }
}

public class ServiceRecordInput
{
    public int? Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Service Date")]
    public DateTime Date { get; set; }

    [Required]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Odometer must be zero or greater.")]
    [Display(Name = "Odometer Reading")]
    public int? Odometer { get; set; }

    [Range(0, 999999999, ErrorMessage = "Cost cannot be negative.")]
    [Display(Name = "Service Cost")]
    public decimal? Cost { get; set; }

    public string? Vendor { get; set; }

    public List<ExtraField> ExtraFields { get; set; } = new();
}

// TODO: include tax, supply linkage, and attachments in later phases.
