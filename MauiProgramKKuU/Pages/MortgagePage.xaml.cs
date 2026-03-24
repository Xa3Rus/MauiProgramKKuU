using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class MortgagePage : ContentPage
{
    private List<Models.PaymentScheduleItem> _lastSchedule = [];

    public MortgagePage()
    {
        InitializeComponent();
        PaymentTypePicker.SelectedIndex = 0;
        PresetPicker.SelectedIndex = 0;
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        try
        {
            string priceText = PropertyPriceEntry.Text?.Replace(',', '.');
            string initialText = InitialPaymentEntry.Text?.Replace(',', '.');
            string rateText = RateEntry.Text?.Replace(',', '.');
            string monthsText = MonthsEntry.Text;

            if (!double.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out double propertyPrice))
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidAmount"), LocalizationService.T("Ok"));
                return;
            }

            if (!double.TryParse(initialText, NumberStyles.Any, CultureInfo.InvariantCulture, out double initialPayment))
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

            double loanAmount = propertyPrice - initialPayment;

            if (propertyPrice <= 0 || initialPayment < 0 || rate < 0 || months <= 0 || loanAmount <= 0)
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
                return;
            }

            var settings = AppSettingsService.Get();
            LoanAmountLabel.Text = $"Сумма ипотеки: {loanAmount:F2} {settings.CurrencySymbol}";

            if (PaymentTypePicker.SelectedIndex == 0)
            {
                var result = LoanCalculator.CalculateAnnuity(loanAmount, rate, months);
                _lastSchedule = LoanCalculator.BuildAnnuitySchedule(loanAmount, rate, months);
                RenderResult(result.MonthlyPayment, result.TotalPayment, result.Overpayment, loanAmount, rate, months, "Аннуитетный");
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(loanAmount, rate, months);
                _lastSchedule = LoanCalculator.BuildDifferentiatedSchedule(loanAmount, rate, months);
                RenderResult(result.FirstPayment, result.TotalPayment, result.Overpayment, loanAmount, rate, months, "Дифференцированный", true);
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
            ProductType = "Ипотека",
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
                PropertyPriceEntry.Text = "3500000";
                InitialPaymentEntry.Text = "700000";
                RateEntry.Text = "11.5";
                MonthsEntry.Text = "180";
                break;
            case 2:
                PropertyPriceEntry.Text = "5000000";
                InitialPaymentEntry.Text = "1500000";
                RateEntry.Text = "8.5";
                MonthsEntry.Text = "240";
                break;
            case 3:
                PropertyPriceEntry.Text = "9000000";
                InitialPaymentEntry.Text = "3000000";
                RateEntry.Text = "10";
                MonthsEntry.Text = "300";
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

        var filePath = await CsvExportService.ExportScheduleAsync(_lastSchedule, "mortgage_schedule");
        await DisplayAlert(LocalizationService.T("ExportDone"), $"{LocalizationService.T("CsvSaved")}: {filePath}", LocalizationService.T("Ok"));
    }

    private async void OnExportPdfClicked(object sender, EventArgs e)
    {
        if (_lastSchedule.Count == 0)
        {
            await DisplayAlert(LocalizationService.T("Warning"), LocalizationService.T("CalculateFirst"), LocalizationService.T("Ok"));
            return;
        }

        var filePath = await PdfExportService.ExportScheduleAsync(_lastSchedule, "Mortgage Schedule");
        await DisplayAlert(LocalizationService.T("ExportDone"), $"{LocalizationService.T("PdfSaved")}: {filePath}", LocalizationService.T("Ok"));
    }
}