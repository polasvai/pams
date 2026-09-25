using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using POMS.Data.Models;

namespace POMS.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded) { await signInManager.SignInAsync(user, isPersistent: true); return LocalRedirect("/Dashboard"); }
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return Page();
    }

    public sealed class InputModel
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
        [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match.")] public string ConfirmPassword { get; set; } = string.Empty;
    }
}
