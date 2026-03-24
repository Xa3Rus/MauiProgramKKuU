namespace MauiProgramKKuU.Services;

public static class ThemeService
{
    public const string Light = "Light";
    public const string Dark = "Dark";
    public const string Purple = "Purple";

    public static IReadOnlyList<string> AvailableThemes => [Light, Dark, Purple];

    public static void ApplyTheme(string? theme)
    {
        var normalized = (theme ?? Dark).Trim();
        if (!AvailableThemes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            normalized = Dark;
        }

        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        if (normalized.Equals(Light, StringComparison.OrdinalIgnoreCase))
        {
            app.UserAppTheme = AppTheme.Light;
            SetPalette("#F4F6FB", "#FFFFFF", "#EEF2FF", "#2563EB", "#111827", "#6B7280", "#DBE3FF", "#1D4ED8");
            return;
        }

        if (normalized.Equals(Purple, StringComparison.OrdinalIgnoreCase))
        {
            app.UserAppTheme = AppTheme.Dark;
            SetPalette("#140A27", "#23123D", "#2D1850", "#A855F7", "#F5EEFF", "#CDB7F8", "#3A2463", "#9333EA");
            return;
        }

        app.UserAppTheme = AppTheme.Dark;
        SetPalette("#0D1028", "#1A2342", "#24345E", "#4F46E5", "#F1F5F9", "#B8C3D9", "#2A365F", "#4338CA");
    }

    private static void SetPalette(
        string pageBackground,
        string cardBackground,
        string altCardBackground,
        string accentColor,
        string textColor,
        string subtextColor,
        string inputBackground,
        string buttonBackground)
    {
        var resources = Application.Current!.Resources;
        resources["AppBackgroundColor"] = Color.FromArgb(pageBackground);
        resources["CardBackgroundColor"] = Color.FromArgb(cardBackground);
        resources["AltCardBackgroundColor"] = Color.FromArgb(altCardBackground);
        resources["AccentColor"] = Color.FromArgb(accentColor);
        resources["AppTextColor"] = Color.FromArgb(textColor);
        resources["AppSubtextColor"] = Color.FromArgb(subtextColor);
        resources["InputBackgroundColor"] = Color.FromArgb(inputBackground);
        resources["PrimaryButtonColor"] = Color.FromArgb(buttonBackground);
    }
}
