using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using POMS.Data.Models;
using POMS.Web.Models;

namespace POMS.Web.Controllers;

[AllowAnonymous]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var identifier = model.UsernameOrEmail.Trim();

        // Lookup user by Username OR Email
        var user = await userManager.FindByNameAsync(identifier)
                   ?? await userManager.FindByEmailAsync(identifier);

        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // Attempt password verification
        var passwordCheck = await signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: false);

        if (passwordCheck.Succeeded)
        {
            await signInManager.SignInAsync(user, isPersistent: model.RememberMe);
            logger.LogInformation("User {UserName} successfully logged in.", user.UserName);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (passwordCheck.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account is temporarily locked out. Please try again later.");
            return View(model);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await userManager.FindByNameAsync(model.Username)
                           ?? await userManager.FindByEmailAsync(model.Email);

        if (existingUser is not null)
        {
            ModelState.AddModelError(string.Empty, "A user with this username or email already exists. Please choose another or sign in.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Username.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? model.Username : model.DisplayName.Trim()
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
            await signInManager.SignInAsync(user, isPersistent: true);
            logger.LogInformation("New user {UserName} created and signed in.", user.UserName);
            return RedirectToLocal(model.ReturnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }



    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Dashboard");
    }
}
