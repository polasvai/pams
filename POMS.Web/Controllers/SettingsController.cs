using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POMS.Web.Models;
using POMS.Web.Services;

namespace POMS.Web.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class SettingsController : Controller
{
    private readonly SettingsService _settingsService;

    public SettingsController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public IActionResult Index()
    {
        return View(_settingsService.GetSettings());
    }

    [HttpPost]
    public async Task<IActionResult> Index(SiteSettings model, IFormFile? logoFile, [FromServices] IWebHostEnvironment env)
    {
        if (ModelState.IsValid)
        {
            var existingSettings = _settingsService.GetSettings();
            
            // Preserve the existing slides (they are managed in separate actions)
            model.Slides = existingSettings.Slides;
            
            // Preserve logo if a new one isn't uploaded and the form didn't pass it back
            if (string.IsNullOrEmpty(model.LogoUrl))
            {
                model.LogoUrl = existingSettings.LogoUrl;
            }

            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = "logo_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(logoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                model.LogoUrl = "/uploads/" + fileName;
            }

            _settingsService.SaveSettings(model);
            TempData["Success"] = "Project settings updated successfully.";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddSlide(string title, string subtitle, IFormFile? slideImage, [FromServices] IWebHostEnvironment env)
    {
        if (slideImage != null && slideImage.Length > 0)
        {
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            var fileName = "slide_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(slideImage.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await slideImage.CopyToAsync(stream);
            }
            
            var settings = _settingsService.GetSettings();
            settings.Slides.Add(new SliderSlide {
                Title = title,
                Subtitle = subtitle,
                ImageUrl = "/uploads/" + fileName
            });
            _settingsService.SaveSettings(settings);
            TempData["Success"] = "Slide added successfully.";
        }
        else
        {
            TempData["Error"] = "Please select an image file.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult DeleteSlide(string id)
    {
        var settings = _settingsService.GetSettings();
        var slide = settings.Slides.FirstOrDefault(s => s.Id == id);
        if (slide != null)
        {
            settings.Slides.Remove(slide);
            _settingsService.SaveSettings(settings);
            TempData["Success"] = "Slide removed successfully.";
        }
        return RedirectToAction(nameof(Index));
    }
}
