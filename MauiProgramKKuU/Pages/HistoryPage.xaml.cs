using MauiProgramKKuU.Services;

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
            .Select(x => new
            {
                x.ProductType,
                x.PaymentType,
                CreatedAtLocalText = x.CreatedAtUtc.ToLocalTime().ToString("g"),
                MainLine = $"{LocalizationService.T("Amount")}: {x.Amount:F2} {settings.CurrencySymbol} | {LocalizationService.T("Rate")}: {x.Rate:F2}% | {LocalizationService.T("Term")}: {x.Months} {LocalizationService.T("MonthsShort")}",
                TotalLine = $"{LocalizationService.T("Payment")}: {x.MonthlyPayment:F2} {settings.CurrencySymbol} | {LocalizationService.T("Total")}: {x.TotalPayment:F2} {settings.CurrencySymbol} | {LocalizationService.T("Overpayment")}: {x.Overpayment:F2} {settings.CurrencySymbol}"
            })
            .ToList();

        HistoryCollectionView.ItemsSource = items;
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
