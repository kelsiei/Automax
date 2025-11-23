namespace CarCareTracker.Models.User;

public class UserData
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsRootUser { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string PasswordHash { get; set; } = string.Empty;
}
