using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class MortgagePage : ContentPage
{
    public MortgagePage()
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
            LoanAmountLabel.Text = $"{LocalizationService.T("MortgageAmount")}: {loanAmount:F2} {settings.CurrencySymbol}";

            if (PaymentTypePicker.SelectedIndex == 0)
            {
                var result = LoanCalculator.CalculateAnnuity(loanAmount, rate, months);
                RenderResult(result.MonthlyPayment, result.TotalPayment, result.Overpayment, loanAmount, rate, months, LocalizationService.T("Annuity"));
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(loanAmount, rate, months);
                RenderResult(result.FirstPayment, result.TotalPayment, result.Overpayment, loanAmount, rate, months, LocalizationService.T("Differentiated"), true);
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
            ProductType = LocalizationService.T("Mortgage"),
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

    private void ApplyLocalization()
    {
        Title = LocalizationService.T("Mortgage");
        PageTitleLabel.Text = LocalizationService.T("MortgageCalculator");
        PropertyPriceLabel.Text = LocalizationService.T("PropertyPrice");
        PropertyPriceEntry.Placeholder = LocalizationService.T("ExamplePropertyPrice");
        InitialPaymentLabel.Text = LocalizationService.T("InitialPayment");
        InitialPaymentEntry.Placeholder = LocalizationService.T("ExampleInitialPayment");
        RateLabel.Text = LocalizationService.T("InterestRate");
        RateEntry.Placeholder = LocalizationService.T("ExampleRateMortgage");
        MonthsLabel.Text = LocalizationService.T("TermMonths");
        MonthsEntry.Placeholder = LocalizationService.T("ExampleMonthsMortgage");
        PaymentTypeLabel.Text = LocalizationService.T("PaymentType");
        PresetLabel.Text = LocalizationService.T("QuickPresets");
        CalculateButton.Text = LocalizationService.T("Calculate");
        ResultHeaderLabel.Text = LocalizationService.T("Result");
        LoanAmountLabel.Text = $"{LocalizationService.T("MortgageAmount")}: -";
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
        PresetPicker.Items.Add(LocalizationService.T("EconomyPreset"));
        PresetPicker.Items.Add(LocalizationService.T("FamilyPreset"));
        PresetPicker.Items.Add(LocalizationService.T("BusinessPreset"));
        if (PresetPicker.SelectedIndex < 0)
        {
            PresetPicker.SelectedIndex = 0;
        }
    }
}