namespace CarCareTracker.Models.Settings;

public class UserColumnPreference
{
    public string? TableName { get; set; }
    public string? ColumnName { get; set; }
    public bool IsVisible { get; set; }
    public int DisplayOrder { get; set; }
}
