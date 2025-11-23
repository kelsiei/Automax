using System.ComponentModel.DataAnnotations;

namespace CarCareTracker.Models.Admin;

public class AdminUserEditModel
{
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Display(Name = "Username")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    [Display(Name = "Email address")]
    public string EmailAddress { get; set; } = string.Empty;

    [Display(Name = "Administrator")]
    public bool IsAdmin { get; set; }

    [Display(Name = "Root user")]
    public bool IsRootUser { get; set; }

    [MinLength(8)]
    [MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$", ErrorMessage = "Password must be at least 8 characters and include at least one uppercase letter, one lowercase letter, and one number.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password (optional)")]
    public string? NewPassword { get; set; }

    [MinLength(8)]
    [MaxLength(100)]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    [Display(Name = "Confirm new password")]
    public string? ConfirmNewPassword { get; set; }
}
