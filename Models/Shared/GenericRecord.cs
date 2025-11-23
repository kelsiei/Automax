using CarCareTracker.Enum;

namespace CarCareTracker.Models.Shared;

public class GenericRecord
{
    public int Id { get; set; }
    public DateTime? Date { get; set; }
    public decimal? Cost { get; set; }
    public string? Description { get; set; }
    public List<ExtraField>? ExtraFields { get; set; }
    // TODO: expand with additional fields (e.g., mileage, tags, files) for import/export scenarios.
}
