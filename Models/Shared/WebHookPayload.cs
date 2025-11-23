namespace CarCareTracker.Models.Shared;

public class WebHookPayload
{
    public string? EventType { get; set; }
    public string? Message { get; set; }
    public string? VehicleName { get; set; }
    public int? VehicleId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    // TODO: include user context or record identifiers if needed by downstream webhooks.
}
