using MauiProgramKKuU.Models;
using System.Text.Json;

namespace MauiProgramKKuU.Services;

public static class AppSettingsService
{
    private const string SettingsKey = "app_settings_v1";

    public static AppSettings Get()
    {
        AppSettings settings;
        try
        {
            var json = Preferences.Get(SettingsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                settings = new AppSettings();
            }
            else
            {
                settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            settings = new AppSettings();
        }

        if (string.IsNullOrWhiteSpace(settings.CurrencySymbol))
        {
            settings.CurrencySymbol = "Br";
        }

        if (string.IsNullOrWhiteSpace(settings.Theme))
        {
            settings.Theme = settings.UseDarkTheme ? "Dark" : "Light";
        }

        return settings;
    }

    public static void Save(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.CurrencySymbol))
        {
            settings.CurrencySymbol = "Br";
        }

        if (string.IsNullOrWhiteSpace(settings.Theme))
        {
            settings.Theme = "Dark";
        }

        settings.UseDarkTheme = settings.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
        var json = JsonSerializer.Serialize(settings);
        Preferences.Set(SettingsKey, json);
    }
}
