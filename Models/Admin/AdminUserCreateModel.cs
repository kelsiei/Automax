using System.ComponentModel.DataAnnotations;

namespace CarCareTracker.Models.Admin;

public class AdminUserCreateModel
{
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

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$", ErrorMessage = "Password must be at least 8 characters and include at least one uppercase letter, one lowercase letter, and one number.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
