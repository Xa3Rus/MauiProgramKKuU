using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class ComparePage : ContentPage
{
    public ComparePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = LocalizationService.T("Compare");
        CompareTitleLabel.Text = LocalizationService.T("CompareThreeScenarios");
        AmountEntry.Placeholder = LocalizationService.T("Amount");
        RateEntry.Placeholder = LocalizationService.T("RatePercent");
        MonthsEntry.Placeholder = LocalizationService.T("BaseTermMonths");
        CompareButton.Text = LocalizationService.T("CompareButton");
    }

    private async void OnCompareClicked(object sender, EventArgs e)
    {
        if (!double.TryParse((AmountEntry.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ||
            !double.TryParse((RateEntry.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
            !int.TryParse(MonthsEntry.Text, out var months) ||
            amount <= 0 || rate < 0 || months <= 0)
        {
            await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
            return;
        }

        var settings = AppSettingsService.Get();
        var terms = new[] { Math.Max(1, months - 12), months, months + 12 };
        var r1 = LoanCalculator.CalculateAnnuity(amount, rate, terms[0]);
        var r2 = LoanCalculator.CalculateAnnuity(amount, rate, terms[1]);
        var r3 = LoanCalculator.CalculateAnnuity(amount, rate, terms[2]);

        Scenario1Label.Text = $"{terms[0]} {LocalizationService.T("MonthsShort")}: {r1.MonthlyPayment:F2} {settings.CurrencySymbol}/{LocalizationService.T("MonthShort")} | {LocalizationService.T("Overpayment").ToLowerInvariant()} {r1.Overpayment:F2} {settings.CurrencySymbol}";
        Scenario2Label.Text = $"{terms[1]} {LocalizationService.T("MonthsShort")}: {r2.MonthlyPayment:F2} {settings.CurrencySymbol}/{LocalizationService.T("MonthShort")} | {LocalizationService.T("Overpayment").ToLowerInvariant()} {r2.Overpayment:F2} {settings.CurrencySymbol}";
        Scenario3Label.Text = $"{terms[2]} {LocalizationService.T("MonthsShort")}: {r3.MonthlyPayment:F2} {settings.CurrencySymbol}/{LocalizationService.T("MonthShort")} | {LocalizationService.T("Overpayment").ToLowerInvariant()} {r3.Overpayment:F2} {settings.CurrencySymbol}";
    }
}
