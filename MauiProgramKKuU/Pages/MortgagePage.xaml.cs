using MauiProgramKKuU.Services;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MauiProgramKKuU.Pages;

public partial class MortgagePage : ContentPage
{
    private CancellationTokenSource? _liveRecalcCts;
    private const int LiveRecalcDelayMs = 350;

    // Split bar state (Сумма ипотеки vs Переплата).
    private double _splitLeft;
    private double _splitRight;

    public MortgagePage()
    {
        InitializeComponent();
        PaymentTypePicker.SelectedIndex = 0;
        PresetPicker.SelectedIndex = 0;

        // Ensure split widths update after layout measurement.
        SplitBarBorder.SizeChanged += (_, __) => UpdateSplitBarWidths();
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
            if (!TryReadInputs(out var propertyPrice, out var initialPayment, out var rate, out var months))
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
                return;
            }

            double loanAmount = propertyPrice - initialPayment;

            var settings = AppSettingsService.Get();
            LoanAmountLabel.Text = $"{LocalizationService.T("MortgageAmount")}: {loanAmount:F2} {settings.CurrencySymbol}";

            if (PaymentTypePicker.SelectedIndex == 0)
            {
                var result = LoanCalculator.CalculateAnnuity(loanAmount, rate, months);
                RenderResult(result.MonthlyPayment, result.TotalPayment, result.Overpayment, loanAmount, rate, months, LocalizationService.T("Annuity"), saveToHistory: true);
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(loanAmount, rate, months);
                RenderResult(result.FirstPayment, result.TotalPayment, result.Overpayment, loanAmount, rate, months, LocalizationService.T("Differentiated"), isFirstPayment: true, saveToHistory: true);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(LocalizationService.T("Error"), ex.Message, LocalizationService.T("Ok"));
        }
    }

    private void RenderResult(
        double payment,
        double total,
        double overpayment,
        double amount,
        double rate,
        int months,
        string paymentType,
        bool isFirstPayment = false,
        bool saveToHistory = false)
    {
        var settings = AppSettingsService.Get();
        var digits = settings.RoundingDigits;
        var currency = settings.CurrencySymbol;

        if (PaymentTypePicker.SelectedIndex == 0)
        {
            MonthlyPaymentLabel.Text = $"{LocalizationService.T("MonthlyPayment")}: {Math.Round(payment, digits):F2} {currency}";
        }
        else
        {
            // For differentiated we show "first -> last" monthly payment.
            var monthlyRate = rate / 12.0 / 100.0;
            var principalPart = amount / months;
            var lastPayment = principalPart + principalPart * monthlyRate;

            MonthlyPaymentLabel.Text =
                $"{LocalizationService.T("MonthlyPayment")}: {Math.Round(payment, digits):F2} -> {Math.Round(lastPayment, digits):F2} {currency}";
        }
        TotalPaymentLabel.Text = $"{LocalizationService.T("TotalPayment")}: {Math.Round(total, digits):F2} {settings.CurrencySymbol}";
        OverpaymentLabel.Text = $"{LocalizationService.T("Overpayment")}: {Math.Round(overpayment, digits):F2} {settings.CurrencySymbol}";

        // Segmented bar: loan amount vs overpayment.
        var left = Math.Max(0, amount);
        var right = Math.Max(0, overpayment);
        var sum = left + right;

        if (sum <= 0)
        {
            SplitBarLegend.IsVisible = false;
            SplitBarBorder.IsVisible = false;
        }
        else
        {
            SplitLeftLegendLabel.Text = LocalizationService.T("MortgageAmount");
            SplitRightLegendLabel.Text = LocalizationService.T("Overpayment");

            SplitBarLegend.IsVisible = true;
            SplitBarBorder.IsVisible = true;

            _splitLeft = left;
            _splitRight = right;
            UpdateSplitBarWidths();
        }

        if (saveToHistory)
        {
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

    private void OnInputSelectionChanged(object sender, EventArgs e)
    {
        ScheduleLiveRecalculate();
    }

    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
    {
        ScheduleLiveRecalculate();
    }

    private async void ScheduleLiveRecalculate()
    {
        try
        {
            _liveRecalcCts?.Cancel();
            _liveRecalcCts = new CancellationTokenSource();
            var token = _liveRecalcCts.Token;

            await Task.Delay(LiveRecalcDelayMs, token);

            if (!TryReadInputs(out var propertyPrice, out var initialPayment, out var rate, out var months))
            {
                RenderEmptyResult();
                return;
            }

            var loanAmount = propertyPrice - initialPayment;
            if (loanAmount <= 0)
            {
                RenderEmptyResult();
                return;
            }

            LoanAmountLabel.Text = $"{LocalizationService.T("MortgageAmount")}: {loanAmount:F2} {AppSettingsService.Get().CurrencySymbol}";

            if (PaymentTypePicker.SelectedIndex == 0)
            {
                var result = LoanCalculator.CalculateAnnuity(loanAmount, rate, months);
                RenderResult(result.MonthlyPayment, result.TotalPayment, result.Overpayment, loanAmount, rate, months, LocalizationService.T("Annuity"));
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(loanAmount, rate, months);
                RenderResult(result.FirstPayment, result.TotalPayment, result.Overpayment, loanAmount, rate, months, LocalizationService.T("Differentiated"), isFirstPayment: true);
            }
        }
        catch (TaskCanceledException)
        {
            // expected during fast typing
        }
        catch
        {
            RenderEmptyResult();
        }
    }

    private void RenderEmptyResult()
    {
        var settings = AppSettingsService.Get();
        LoanAmountLabel.Text = $"{LocalizationService.T("MortgageAmount")}: -";
        MonthlyPaymentLabel.Text = $"{LocalizationService.T("MonthlyPayment")}: -";
        TotalPaymentLabel.Text = $"{LocalizationService.T("TotalPayment")}: -";
        OverpaymentLabel.Text = $"{LocalizationService.T("Overpayment")}: -";

        SplitBarLegend.IsVisible = false;
        SplitBarBorder.IsVisible = false;

        _splitLeft = 0;
        _splitRight = 0;
    }

    private void UpdateSplitBarWidths()
    {
        if (!SplitBarBorder.IsVisible)
        {
            return;
        }

        var total = _splitLeft + _splitRight;
        if (total <= 0)
        {
            return;
        }

        var w = SplitBarGrid.Width;
        if (w <= 0)
        {
            return;
        }

        var inner = Math.Max(0, w - SplitBarGrid.ColumnSpacing);
        var leftW = inner * (_splitLeft / total);
        var rightW = Math.Max(0, inner - leftW);

        SplitBarAmountFill.WidthRequest = leftW;
        SplitBarOverpaymentFill.WidthRequest = rightW;
    }

    private bool TryReadInputs(out double propertyPrice, out double initialPayment, out double rate, out int months)
    {
        propertyPrice = 0;
        initialPayment = 0;
        rate = 0;
        months = 0;

        var priceText = (PropertyPriceEntry.Text ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace(',', '.')
            .Trim();
        var initialText = (InitialPaymentEntry.Text ?? string.Empty)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace(',', '.')
            .Trim();
        var rateText = (RateEntry.Text ?? string.Empty)
            .Replace("%", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace(',', '.')
            .Trim();
        var monthsText = (MonthsEntry.Text ?? string.Empty).Trim();

        if (!double.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out propertyPrice) ||
            !double.TryParse(initialText, NumberStyles.Any, CultureInfo.InvariantCulture, out initialPayment) ||
            !double.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out rate) ||
            !int.TryParse(monthsText, NumberStyles.Any, CultureInfo.InvariantCulture, out months))
        {
            return false;
        }

        var loanAmount = propertyPrice - initialPayment;
        if (propertyPrice <= 0 || initialPayment < 0 || rate < 0 || months <= 0 || loanAmount <= 0)
        {
            return false;
        }

        return true;
    }

    private async void OnOpenAnalyticsClicked(object sender, EventArgs e)
    {
        try
        {
            string priceText = (PropertyPriceEntry.Text ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty)
                .Replace(',', '.')
                .Trim();
            string initialText = (InitialPaymentEntry.Text ?? string.Empty)
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty)
                .Replace(',', '.')
                .Trim();
            string rateText = (RateEntry.Text ?? string.Empty)
                .Replace("%", string.Empty)
                .Replace(" ", string.Empty)
                .Replace("\u00A0", string.Empty)
                .Replace(',', '.')
                .Trim();
            string monthsText = (MonthsEntry.Text ?? string.Empty).Trim();

            if (!double.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out var propertyPrice) ||
                !double.TryParse(initialText, NumberStyles.Any, CultureInfo.InvariantCulture, out var initialPayment) ||
                !double.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
                !int.TryParse(monthsText, NumberStyles.Any, CultureInfo.InvariantCulture, out var months) ||
                propertyPrice <= 0 || initialPayment < 0 || rate < 0 || months <= 0)
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
                return;
            }

            if (propertyPrice - initialPayment <= 0)
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
                return;
            }

            var paymentTypeKey = PaymentTypePicker.SelectedIndex == 0 ? "Annuity" : "Differentiated";

            var loanAmount = propertyPrice - initialPayment;

            // Save current snapshot into history.
            if (paymentTypeKey == "Annuity")
            {
                var result = LoanCalculator.CalculateAnnuity(loanAmount, rate, months);
                CalculationHistoryService.Add(new Models.LoanHistoryItem
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    ProductType = LocalizationService.T("Mortgage"),
                    Amount = loanAmount,
                    Rate = rate,
                    Months = months,
                    PaymentType = LocalizationService.T("Annuity"),
                    MonthlyPayment = result.MonthlyPayment,
                    TotalPayment = result.TotalPayment,
                    Overpayment = result.Overpayment
                });
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(loanAmount, rate, months);
                CalculationHistoryService.Add(new Models.LoanHistoryItem
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    ProductType = LocalizationService.T("Mortgage"),
                    Amount = loanAmount,
                    Rate = rate,
                    Months = months,
                    PaymentType = LocalizationService.T("Differentiated"),
                    MonthlyPayment = result.FirstPayment,
                    TotalPayment = result.TotalPayment,
                    Overpayment = result.Overpayment
                });
            }

            await Shell.Current.GoToAsync(
                $"{nameof(AnalyticsPage)}?mode=mortgage&price={propertyPrice.ToString(CultureInfo.InvariantCulture)}&initial={initialPayment.ToString(CultureInfo.InvariantCulture)}&rate={rate.ToString(CultureInfo.InvariantCulture)}&months={months}&paymentType={paymentTypeKey}");
        }
        catch (Exception ex)
        {
            await DisplayAlert(LocalizationService.T("Error"), ex.Message, LocalizationService.T("Ok"));
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
        ResultHeaderLabel.Text = LocalizationService.T("Result");
        LoanAmountLabel.Text = $"{LocalizationService.T("MortgageAmount")}: -";
        MonthlyPaymentLabel.Text = $"{LocalizationService.T("MonthlyPayment")}: -";
        TotalPaymentLabel.Text = $"{LocalizationService.T("TotalPayment")}: -";
        OverpaymentLabel.Text = $"{LocalizationService.T("Overpayment")}: -";
        OpenAnalyticsButton.Text = LocalizationService.T("Analytics");
        RenderEmptyResult();

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