using System.ComponentModel.DataAnnotations;
using CarCareTracker.Models.Shared;

namespace CarCareTracker.Models.GasRecord;

public class GasRecord
{
    public int Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fill-up Date")]
    public DateTime Date { get; set; }

    // Odometer reading at fill-up
    public int? Odometer { get; set; }

    // Volume of fuel, e.g., liters or gallons
    [Required]
    [Range(0.01, 999999, ErrorMessage = "Volume must be greater than zero.")]
    [Display(Name = "Fuel Volume")]
    public decimal Volume { get; set; }

    // Price per unit of fuel
    [Required]
    [Range(0.01, 999999, ErrorMessage = "Price per unit must be greater than zero.")]
    [Display(Name = "Price Per Unit")]
    public decimal PricePerUnit { get; set; }

    // Total cost for this fill-up
    [Range(0, 999999999, ErrorMessage = "Total cost cannot be negative.")]
    [Display(Name = "Total Cost")]
    public decimal TotalCost { get; set; }

    public bool IsFullFillUp { get; set; }

    public string? Notes { get; set; }

    public List<ExtraField> ExtraFields { get; set; } = new();
}

public class GasRecordInput
{
    public int? Id { get; set; }
    public int VehicleId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Fill-up Date")]
    public DateTime Date { get; set; }
    public int? Odometer { get; set; }

    [Required]
    [Range(0.01, 999999, ErrorMessage = "Volume must be greater than zero.")]
    [Display(Name = "Fuel Volume")]
    public decimal Volume { get; set; }

    [Required]
    [Range(0.01, 999999, ErrorMessage = "Price per unit must be greater than zero.")]
    [Display(Name = "Price Per Unit")]
    public decimal PricePerUnit { get; set; }
    public bool IsFullFillUp { get; set; }

    public string? Notes { get; set; }

    public List<ExtraField> ExtraFields { get; set; } = new();
}

// TODO: add missed fill-up metadata and calculated MPG fields in later phases.
