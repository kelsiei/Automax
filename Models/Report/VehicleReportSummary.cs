namespace CarCareTracker.Models.Report;

public class VehicleReportSummary
{
    public int VehicleId { get; set; }

    public int Year { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;

    public int? LastReportedMileage { get; set; }

    public DateTime? LastServiceDate { get; set; }
    public DateTime? LastGasDate { get; set; }

    public decimal? TotalCost { get; set; }
    public decimal? CostPerMile { get; set; }

    public int ActivePlanCount { get; set; }
    public int NoteCount { get; set; }
    public int DocumentCount { get; set; }

    public bool HasUrgentReminders { get; set; }
}
