namespace CarCareTracker.Models.API;

public class WhoAmIResponse
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
    public bool IsRootUser { get; set; }
}
