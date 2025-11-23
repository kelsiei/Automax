using CarCareTracker.Models.Vehicle;

namespace CarCareTracker.Models.API;

public class VehicleInfo
{
    public int Id { get; set; }

    public int Year { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public string? LicensePlate { get; set; }

    public int? LastReportedMileage { get; set; }
    public decimal? TotalCost { get; set; }
    public decimal? CostPerMile { get; set; }

    public bool HasUrgentReminders { get; set; }

    public static VehicleInfo FromViewModel(VehicleViewModel vm)
    {
        var v = vm.Vehicle;

        return new VehicleInfo
        {
            Id = v.Id,
            Year = v.Year,
            Make = v.Make,
            Model = v.Model,
            LicensePlate = v.LicensePlate,
            LastReportedMileage = vm.LastReportedMileage.HasValue
                ? (int?)Convert.ToInt32(vm.LastReportedMileage.Value)
                : null,
            TotalCost = vm.TotalCost,
            CostPerMile = vm.CostPerMile,
            HasUrgentReminders = vm.HasUrgentReminders
        };
    }
}
