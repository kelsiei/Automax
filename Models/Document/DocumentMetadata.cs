namespace Automax.Models.Document;

public class DocumentMetadata
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
