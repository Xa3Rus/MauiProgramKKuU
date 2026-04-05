using MauiProgramKKuU.Services;
using MauiProgramKKuU.Models;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = LocalizationService.T("History");
        HistoryTitleLabel.Text = LocalizationService.T("LatestCalculations");
        ClearButton.Text = LocalizationService.T("ClearHistory");
        LoadHistory();
    }

    private void LoadHistory()
    {
        var settings = AppSettingsService.Get();
        var items = CalculationHistoryService.GetAll()
            .Select(x => new SavedCalculationRow
            {
                ProductType = x.ProductType,
                PaymentType = x.PaymentType,
                Amount = x.Amount,
                Rate = x.Rate,
                Months = x.Months,
                CreatedAtLocalText = x.CreatedAtUtc.ToLocalTime().ToString("g"),
                MainLine = $"{LocalizationService.T("Amount")}: {x.Amount:F2} {settings.CurrencySymbol} | {LocalizationService.T("Rate")}: {x.Rate:F2}% | {LocalizationService.T("Term")}: {x.Months} {LocalizationService.T("MonthsShort")}",
                TotalLine = $"{LocalizationService.T("Payment")}: {x.MonthlyPayment:F2} {settings.CurrencySymbol} | {LocalizationService.T("Total")}: {x.TotalPayment:F2} {settings.CurrencySymbol} | {LocalizationService.T("Overpayment")}: {x.Overpayment:F2} {settings.CurrencySymbol}"
            })
            .ToList();

        HistoryCollectionView.ItemsSource = items;
    }

    private async void OnHistorySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.CurrentSelection is null || e.CurrentSelection.Count == 0)
            {
                return;
            }

            var row = e.CurrentSelection[0] as SavedCalculationRow;
            if (row is null)
            {
                return;
            }

            // ProductType is localized at save-time, so we map using substrings.
            var isMortgage = row.ProductType.Contains("Ипотек", StringComparison.OrdinalIgnoreCase) ||
                              row.ProductType.Contains("Mortgage", StringComparison.OrdinalIgnoreCase);

            var paymentType = row.PaymentType;

            await Shell.Current.GoToAsync(
                isMortgage
                    ? $"{nameof(AnalyticsPage)}?mode=mortgage&price={row.Amount.ToString(CultureInfo.InvariantCulture)}&initial=0&rate={row.Rate.ToString(CultureInfo.InvariantCulture)}&months={row.Months}&paymentType={paymentType}"
                    : $"{nameof(AnalyticsPage)}?mode=credit&amount={row.Amount.ToString(CultureInfo.InvariantCulture)}&rate={row.Rate.ToString(CultureInfo.InvariantCulture)}&months={row.Months}&paymentType={paymentType}");
        }
        finally
        {
            HistoryCollectionView.SelectedItem = null;
        }
    }

    private async void OnClearClicked(object sender, EventArgs e)
    {
        var shouldClear = await DisplayAlert(LocalizationService.T("Confirmation"), LocalizationService.T("ClearHistoryQuestion"), LocalizationService.T("Yes"), LocalizationService.T("No"));
        if (!shouldClear)
        {
            return;
        }

        CalculationHistoryService.Clear();
        LoadHistory();
    }
}
