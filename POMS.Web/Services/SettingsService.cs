using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using POMS.Web.Models;

namespace POMS.Web.Services;

public class SettingsService
{
    private readonly string _filePath;
    private SiteSettings _currentSettings;

    public SettingsService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "App_Data", "settings.json");
        LoadSettings();
    }

    public SiteSettings GetSettings() => _currentSettings;

    public void SaveSettings(SiteSettings settings)
    {
        _currentSettings = settings;
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private void LoadSettings()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                _currentSettings = JsonSerializer.Deserialize<SiteSettings>(json) ?? new SiteSettings();
            }
            catch
            {
                _currentSettings = new SiteSettings();
            }
        }
        else
        {
            _currentSettings = new SiteSettings();
            SaveSettings(_currentSettings);
        }
    }
}
