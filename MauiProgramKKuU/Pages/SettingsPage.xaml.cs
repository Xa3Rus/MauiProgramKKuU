using MauiProgramKKuU.Models;
using MauiProgramKKuU.Services;

namespace MauiProgramKKuU.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly Dictionary<string, string> _themeValueByLabel = new();

    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadSettings();
    }

    private void LoadSettings()
    {
        Title = LocalizationService.T("Settings");
        CurrencyLabel.Text = LocalizationService.T("Currency");
        LanguageLabel.Text = LocalizationService.T("Language");
        RoundingLabel.Text = LocalizationService.T("Rounding");
        ThemeLabel.Text = LocalizationService.T("Theme");
        SaveButton.Text = LocalizationService.T("Save");

        _themeValueByLabel.Clear();
        ThemePicker.Items.Clear();
        AddThemeItem(LocalizationService.T("ThemeLight"), ThemeService.Light);
        AddThemeItem(LocalizationService.T("ThemeDark"), ThemeService.Dark);
        AddThemeItem(LocalizationService.T("ThemePurple"), ThemeService.Purple);

        var settings = AppSettingsService.Get();
        CurrencyPicker.SelectedItem = settings.CurrencySymbol;
        LanguagePicker.SelectedItem = settings.Language;
        RoundingPicker.SelectedItem = settings.RoundingDigits.ToString();
        ThemePicker.SelectedItem = _themeValueByLabel.FirstOrDefault(x => x.Value == settings.Theme).Key ?? ThemePicker.Items.FirstOrDefault();
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var settings = new AppSettings
        {
            CurrencySymbol = CurrencyPicker.SelectedItem?.ToString() ?? "Br",
            Language = LanguagePicker.SelectedItem?.ToString() ?? "RU",
            RoundingDigits = int.TryParse(RoundingPicker.SelectedItem?.ToString(), out var digits) ? digits : 2,
            Theme = GetSelectedThemeValue()
        };

        AppSettingsService.Save(settings);
        ThemeService.ApplyTheme(settings.Theme);
        await DisplayAlert(LocalizationService.T("Done"), LocalizationService.T("SettingsSaved"), LocalizationService.T("Ok"));

        if (Application.Current?.Windows.Count > 0)
        {
            Application.Current.Windows[0].Page = new AppShell();
        }
    }

    private void AddThemeItem(string label, string value)
    {
        ThemePicker.Items.Add(label);
        _themeValueByLabel[label] = value;
    }

    private string GetSelectedThemeValue()
    {
        var label = ThemePicker.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(label) && _themeValueByLabel.TryGetValue(label, out var value))
        {
            return value;
        }

        return ThemeService.Dark;
    }
}
