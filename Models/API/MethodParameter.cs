namespace CarCareTracker.Models.API;

public class MethodParameter
{
    public int? Id { get; set; }

    public int? VehicleId { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int? Page { get; set; }
    public int? PageSize { get; set; }

    // TODO: Extend with additional filter fields (tags, urgency, etc.) as needed.
}
