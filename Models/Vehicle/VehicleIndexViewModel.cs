namespace CarCareTracker.Models.Vehicle;

public class VehicleIndexViewModel
{
    public IEnumerable<VehicleViewModel> Vehicles { get; set; } = Enumerable.Empty<VehicleViewModel>();

    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public string SearchTerm { get; set; } = string.Empty;
    public bool ShowOnlyUrgent { get; set; }
}
