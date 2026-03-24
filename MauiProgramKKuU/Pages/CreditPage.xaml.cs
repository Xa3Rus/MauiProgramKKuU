using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class CreditPage : ContentPage
{
    private List<Models.PaymentScheduleItem> _lastSchedule = [];

    public CreditPage()
    {
        InitializeComponent();
        PaymentTypePicker.SelectedIndex = 0;
        PresetPicker.SelectedIndex = 0;
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        try
        {
            string amountText = AmountEntry.Text?.Replace(',', '.');
            string rateText = RateEntry.Text?.Replace(',', '.');
            string monthsText = MonthsEntry.Text;

            if (!double.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out double amount))
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidAmount"), LocalizationService.T("Ok"));
                return;
            }

            if (!double.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out double rate))
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidRate"), LocalizationService.T("Ok"));
                return;
            }

            if (!int.TryParse(monthsText, out int months))
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidMonths"), LocalizationService.T("Ok"));
                return;
            }

            if (amount <= 0 || rate < 0 || months <= 0)
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
                return;
            }

            if (PaymentTypePicker.SelectedIndex == 0)
            {
                var result = LoanCalculator.CalculateAnnuity(amount, rate, months);
                _lastSchedule = LoanCalculator.BuildAnnuitySchedule(amount, rate, months);
                RenderResult(result.MonthlyPayment, result.TotalPayment, result.Overpayment, amount, rate, months, "Аннуитетный");
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(amount, rate, months);
                _lastSchedule = LoanCalculator.BuildDifferentiatedSchedule(amount, rate, months);
                RenderResult(result.FirstPayment, result.TotalPayment, result.Overpayment, amount, rate, months, "Дифференцированный", true);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(LocalizationService.T("Error"), ex.Message, LocalizationService.T("Ok"));
        }
    }

    private void RenderResult(double payment, double total, double overpayment, double amount, double rate, int months, string paymentType, bool isFirstPayment = false)
    {
        var settings = AppSettingsService.Get();
        var digits = settings.RoundingDigits;
        var paymentPrefix = isFirstPayment ? LocalizationService.T("FirstPayment") : LocalizationService.T("MonthlyPayment");

        MonthlyPaymentLabel.Text = $"{paymentPrefix}: {Math.Round(payment, digits):F2} {settings.CurrencySymbol}";
        TotalPaymentLabel.Text = $"{LocalizationService.T("TotalPayment")}: {Math.Round(total, digits):F2} {settings.CurrencySymbol}";
        OverpaymentLabel.Text = $"{LocalizationService.T("Overpayment")}: {Math.Round(overpayment, digits):F2} {settings.CurrencySymbol}";

        ScheduleCollection.ItemsSource = _lastSchedule.Select(s => new
        {
            s.MonthNumber,
            PaymentText = $"Пл: {Math.Round(s.Payment, digits):F2}",
            InterestText = $"%: {Math.Round(s.Interest, digits):F2}",
            DebtText = $"Ост: {Math.Round(s.RemainingDebt, digits):F2}"
        }).ToList();

        CalculationHistoryService.Add(new Models.LoanHistoryItem
        {
            CreatedAtUtc = DateTime.UtcNow,
            ProductType = "Кредит",
            Amount = amount,
            Rate = rate,
            Months = months,
            PaymentType = paymentType,
            MonthlyPayment = payment,
            TotalPayment = total,
            Overpayment = overpayment
        });
    }

    private void OnPresetChanged(object sender, EventArgs e)
    {
        switch (PresetPicker.SelectedIndex)
        {
            case 1:
                AmountEntry.Text = "300000";
                RateEntry.Text = "16";
                MonthsEntry.Text = "36";
                break;
            case 2:
                AmountEntry.Text = "1200000";
                RateEntry.Text = "11";
                MonthsEntry.Text = "60";
                break;
            case 3:
                AmountEntry.Text = "5000000";
                RateEntry.Text = "9.5";
                MonthsEntry.Text = "240";
                break;
        }
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        if (_lastSchedule.Count == 0)
        {
            await DisplayAlert(LocalizationService.T("Warning"), LocalizationService.T("CalculateFirst"), LocalizationService.T("Ok"));
            return;
        }

        var filePath = await CsvExportService.ExportScheduleAsync(_lastSchedule, "credit_schedule");
        await DisplayAlert(LocalizationService.T("ExportDone"), $"{LocalizationService.T("CsvSaved")}: {filePath}", LocalizationService.T("Ok"));
    }

    private async void OnExportPdfClicked(object sender, EventArgs e)
    {
        if (_lastSchedule.Count == 0)
        {
            await DisplayAlert(LocalizationService.T("Warning"), LocalizationService.T("CalculateFirst"), LocalizationService.T("Ok"));
            return;
        }

        var filePath = await PdfExportService.ExportScheduleAsync(_lastSchedule, "Credit Schedule");
        await DisplayAlert(LocalizationService.T("ExportDone"), $"{LocalizationService.T("PdfSaved")}: {filePath}", LocalizationService.T("Ok"));
    }
}