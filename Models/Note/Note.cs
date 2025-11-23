namespace CarCareTracker.Models.Note;

public class Note
{
    public int Id { get; set; }
    public int VehicleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
