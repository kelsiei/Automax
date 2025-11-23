using CarCareTracker.Enum;
using CarCareTracker.Models.Shared;

namespace CarCareTracker.Models.Vehicle;

public class Vehicle
{
    public int Id { get; set; }
    public int Year { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? LicensePlate { get; set; }
    public DateTime? SoldDate { get; set; }
    public bool IsElectric { get; set; }
    public bool IsDiesel { get; set; }
    public bool UseHours { get; set; }
    public bool OdometerOptional { get; set; }
    public string? VehicleIdentifier { get; set; }
    public string? ImageLocation { get; set; }
    public string? MapLocation { get; set; }
    public List<string>? Tags { get; set; }
    public List<DashboardMetric>? DashboardMetrics { get; set; }
    public List<ExtraField>? ExtraFields { get; set; }
    // TODO: add baseline costs, odometer adjustments, and additional spec fields.
}
