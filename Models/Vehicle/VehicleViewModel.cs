namespace CarCareTracker.Models.Vehicle;

public class VehicleViewModel
{
    public Vehicle Vehicle { get; set; } = new();
    public decimal? CostPerMile { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? LastReportedMileage { get; set; }
    public bool HasUrgentReminders { get; set; }
    public DateTime? LastServiceDate { get; set; }
    public DateTime? LastGasDate { get; set; }
    public int ActivePlanCount { get; set; }
    public int NoteCount { get; set; }
    public int DocumentCount { get; set; }
}
