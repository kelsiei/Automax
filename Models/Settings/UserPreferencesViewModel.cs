using System.ComponentModel.DataAnnotations;

namespace Automax.Models.Settings;

public class UserPreferencesViewModel
{
    [Display(Name = "Preferred units")]
    public string PreferredUnits { get; set; } = string.Empty; // e.g., Metric/Imperial

    [Display(Name = "Default landing page")]
    public string DefaultLandingPage { get; set; } = "Dashboard"; // Dashboard or Garage

    [Display(Name = "Show fuel widget on dashboard")]
    public bool ShowFuelWidget { get; set; } = true;
}
