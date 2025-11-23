namespace CarCareTracker.Models.Report;

public class ReportIndexViewModel
{
    public IEnumerable<VehicleReportSummary> VehicleSummaries { get; set; } = Enumerable.Empty<VehicleReportSummary>();

    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public string SearchTerm { get; set; } = string.Empty;
    public bool ShowOnlyUrgent { get; set; }
}
