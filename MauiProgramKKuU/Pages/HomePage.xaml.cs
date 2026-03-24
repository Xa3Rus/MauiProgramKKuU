using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = LocalizationService.T("Home");
        MainTitleLabel.Text = LocalizationService.T("HomeTitle");
        SubtitleLabel.Text = LocalizationService.T("HomeSubtitle");
        CreditCardTitle.Text = LocalizationService.T("Credits");
        MortgageCardTitle.Text = LocalizationService.T("Mortgage");
        QuickCalcTitleLabel.Text = LocalizationService.T("QuickCalculation");
        QuickAmountEntry.Placeholder = LocalizationService.T("Amount");
        QuickMonthsEntry.Placeholder = LocalizationService.T("TermMonths");
        QuickRateEntry.Placeholder = LocalizationService.T("RatePercent");
        QuickCalcButton.Text = LocalizationService.T("Calculate");
        LastCalcTitleLabel.Text = LocalizationService.T("LastCalculation");
        HistoryQuickButton.Text = LocalizationService.T("History");
        CompareQuickButton.Text = LocalizationService.T("Compare");
        SettingsQuickButton.Text = LocalizationService.T("Settings");
        LoadLastCalculation();
    }

    private async void OnCreditTapped(object sender, TappedEventArgs e)
    {
        await AnimateCard(CreditCard);
        await Shell.Current.GoToAsync(nameof(CreditPage));
    }

    private async void OnMortgageTapped(object sender, TappedEventArgs e)
    {
        await AnimateCard(MortgageCard);
        await Shell.Current.GoToAsync(nameof(MortgagePage));
    }

    private async Task AnimateCard(VisualElement card)
    {
        await card.ScaleTo(0.97, 80);
        await card.ScaleTo(1.0, 80);
    }

    private void LoadLastCalculation()
    {
        var settings = AppSettingsService.Get();
        var last = CalculationHistoryService.GetAll().FirstOrDefault();
        if (last is null)
        {
            LastCalcValueLabel.Text = LocalizationService.T("NoCalculationsYet");
            return;
        }

        LastCalcValueLabel.Text =
            $"{last.ProductType}: {last.MonthlyPayment:F2} {settings.CurrencySymbol}/" +
            $"{LocalizationService.T("MonthShort")} | {LocalizationService.T("Overpayment")}: {last.Overpayment:F2} {settings.CurrencySymbol}";
    }

    private async void OnQuickCalculateClicked(object sender, EventArgs e)
    {
        var amountText = (QuickAmountEntry.Text ?? string.Empty).Replace(',', '.');
        var monthsText = QuickMonthsEntry.Text ?? string.Empty;
        var rateText = (QuickRateEntry.Text ?? string.Empty).Replace(',', '.');

        if (!double.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ||
            !int.TryParse(monthsText, out var months) ||
            !double.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
            amount <= 0 || months <= 0 || rate < 0)
        {
            await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
            return;
        }

        var settings = AppSettingsService.Get();
        var result = LoanCalculator.CalculateAnnuity(amount, rate, months);
        QuickCalcResultLabel.Text =
            $"{LocalizationService.T("MonthlyPayment")}: {result.MonthlyPayment:F2} {settings.CurrencySymbol}, " +
            $"{LocalizationService.T("Overpayment")}: {result.Overpayment:F2} {settings.CurrencySymbol}";
    }

    private async void OnHistoryTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(HistoryPage));
    }

    private async void OnCompareTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ComparePage));
    }

    private async void OnSettingsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}