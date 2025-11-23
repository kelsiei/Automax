namespace CarCareTracker.Models.Shared;

public class ExtraField
{
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public CarCareTracker.Enum.ExtraFieldType Type { get; set; } = CarCareTracker.Enum.ExtraFieldType.Text;
    // TODO: add metadata such as content type, options, and validation rules.
}

public class RecordExtraField
{
    public int Id { get; set; }
    public int RecordId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Value { get; set; }
    public CarCareTracker.Enum.ExtraFieldType Type { get; set; } = CarCareTracker.Enum.ExtraFieldType.Text;
    // TODO: add metadata and linkage details as needed.
}

public class UploadedFiles
{
    public List<string> FileNames { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    // TODO: include content type, size, and checksum if required later.
}
