using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class CreditPage : ContentPage
{
    public CreditPage()
    {
        InitializeComponent();
        PaymentTypePicker.SelectedIndex = 0;
        PresetPicker.SelectedIndex = 0;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyLocalization();
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
                RenderResult(result.MonthlyPayment, result.TotalPayment, result.Overpayment, amount, rate, months, LocalizationService.T("Annuity"));
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(amount, rate, months);
                RenderResult(result.FirstPayment, result.TotalPayment, result.Overpayment, amount, rate, months, LocalizationService.T("Differentiated"), true);
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

        CalculationHistoryService.Add(new Models.LoanHistoryItem
        {
            CreatedAtUtc = DateTime.UtcNow,
            ProductType = LocalizationService.T("Credits"),
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

    private void ApplyLocalization()
    {
        Title = LocalizationService.T("Credits");
        PageTitleLabel.Text = LocalizationService.T("CreditCalculator");
        AmountLabel.Text = LocalizationService.T("LoanAmount");
        AmountEntry.Placeholder = LocalizationService.T("ExampleAmount");
        RateLabel.Text = LocalizationService.T("InterestRate");
        RateEntry.Placeholder = LocalizationService.T("ExampleRate");
        MonthsLabel.Text = LocalizationService.T("TermMonths");
        MonthsEntry.Placeholder = LocalizationService.T("ExampleMonths");
        PaymentTypeLabel.Text = LocalizationService.T("PaymentType");
        PresetLabel.Text = LocalizationService.T("QuickPresets");
        CalculateButton.Text = LocalizationService.T("Calculate");
        ResultHeaderLabel.Text = LocalizationService.T("Result");
        MonthlyPaymentLabel.Text = $"{LocalizationService.T("MonthlyPayment")}: -";
        TotalPaymentLabel.Text = $"{LocalizationService.T("TotalPayment")}: -";
        OverpaymentLabel.Text = $"{LocalizationService.T("Overpayment")}: -";

        PaymentTypePicker.Items.Clear();
        PaymentTypePicker.Items.Add(LocalizationService.T("Annuity"));
        PaymentTypePicker.Items.Add(LocalizationService.T("Differentiated"));
        if (PaymentTypePicker.SelectedIndex < 0)
        {
            PaymentTypePicker.SelectedIndex = 0;
        }

        PresetPicker.Items.Clear();
        PresetPicker.Items.Add(LocalizationService.T("None"));
        PresetPicker.Items.Add(LocalizationService.T("ConsumerPreset"));
        PresetPicker.Items.Add(LocalizationService.T("AutoPreset"));
        PresetPicker.Items.Add(LocalizationService.T("MortgageStandardPreset"));
        if (PresetPicker.SelectedIndex < 0)
        {
            PresetPicker.SelectedIndex = 0;
        }
    }
}