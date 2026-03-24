using MauiProgramKKuU.Models;
using MauiProgramKKuU.Services;

namespace MauiProgramKKuU.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = AppSettingsService.Get();
        CurrencyPicker.SelectedItem = settings.CurrencySymbol;
        LanguagePicker.SelectedItem = settings.Language;
        RoundingPicker.SelectedItem = settings.RoundingDigits.ToString();
        DarkThemeSwitch.IsToggled = settings.UseDarkTheme;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var settings = new AppSettings
        {
            CurrencySymbol = CurrencyPicker.SelectedItem?.ToString() ?? "Br",
            Language = LanguagePicker.SelectedItem?.ToString() ?? "RU",
            RoundingDigits = int.TryParse(RoundingPicker.SelectedItem?.ToString(), out var digits) ? digits : 2,
            UseDarkTheme = DarkThemeSwitch.IsToggled
        };

        AppSettingsService.Save(settings);
        Application.Current!.UserAppTheme = settings.UseDarkTheme ? AppTheme.Dark : AppTheme.Light;
        await DisplayAlert("Готово", "Настройки сохранены", "OK");
    }
}
