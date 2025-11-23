namespace CarCareTracker.Models.User;

public class UserAccess
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int VehicleId { get; set; }
    public bool CanEdit { get; set; }
}
