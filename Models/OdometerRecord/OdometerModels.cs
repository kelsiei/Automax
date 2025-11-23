using System.ComponentModel.DataAnnotations;

namespace CarCareTracker.Models.OdometerRecord;

public class OdometerRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Reading Date")]
    public DateTime Date { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Odometer must be zero or greater.")]
    [Display(Name = "Odometer Reading")]
    public int Odometer { get; set; }

    public string? Notes { get; set; }
}

public class OdometerRecordInput
{
    public int? Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Reading Date")]
    public DateTime Date { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Odometer must be zero or greater.")]
    [Display(Name = "Odometer Reading")]
    public int Odometer { get; set; }

    public string? Notes { get; set; }
}

// TODO: add adjustment/multiplier metadata in later phases if needed.
