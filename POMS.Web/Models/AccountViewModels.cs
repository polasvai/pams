using System.ComponentModel.DataAnnotations;

namespace POMS.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your username or email address.")]
    [Display(Name = "Username or Email")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me on this device")]
    public bool RememberMe { get; set; } = true;

    public string? ReturnUrl
    {
        get; set;
    }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Username is required.")]
    [Display(Name = "Username")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Full Name")]
    public string? DisplayName
    {
        get; set;
    }

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Password must be at least 4 characters long.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl
    {
        get; set;
    }
}
