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
                MainLine = $"Сумма: {x.Amount:F2} {settings.CurrencySymbol} | Ставка: {x.Rate:F2}% | Срок: {x.Months} мес.",
                TotalLine = $"Платеж: {x.MonthlyPayment:F2} {settings.CurrencySymbol} | Итого: {x.TotalPayment:F2} {settings.CurrencySymbol} | Переплата: {x.Overpayment:F2} {settings.CurrencySymbol}"
            })
            .ToList();

        HistoryCollectionView.ItemsSource = items;
    }

    private async void OnClearClicked(object sender, EventArgs e)
    {
        var shouldClear = await DisplayAlert("Подтверждение", "Очистить всю историю?", "Да", "Нет");
        if (!shouldClear)
        {
            return;
        }

        CalculationHistoryService.Clear();
        LoadHistory();
    }
}
