using MauiProgramKKuU.Services;
using System.Globalization;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System.Threading;
using System.Threading.Tasks;

namespace MauiProgramKKuU.Pages;

public partial class CreditPage : ContentPage
{
    private CancellationTokenSource? _liveRecalcCts;
    private const int LiveRecalcDelayMs = 350;

    // Split bar state (Сумма кредита vs Переплата).
    private double _splitLeft;
    private double _splitRight;

    public CreditPage()
    {
        InitializeComponent();
        PaymentTypePicker.SelectedIndex = 0;
        PresetPicker.SelectedIndex = 0;

        // Ensure bar widths update after layout measurement.
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
            if (!TryReadInputs(out var amount, out var rate, out var months))
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
                return;
            }

            if (PaymentTypePicker.SelectedIndex == 0)
            {
                var result = LoanCalculator.CalculateAnnuity(amount, rate, months);
                RenderResult(result.MonthlyPayment, result.TotalPayment, result.Overpayment, amount, rate, months, LocalizationService.T("Annuity"), saveToHistory: true);
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(amount, rate, months);
                RenderResult(result.FirstPayment, result.TotalPayment, result.Overpayment, amount, rate, months, LocalizationService.T("Differentiated"), isFirstPayment: true, saveToHistory: true);
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
            SplitLeftLegendLabel.Text = LocalizationService.T("LoanAmount");
            SplitRightLegendLabel.Text = LocalizationService.T("Overpayment");

            SplitBarLegend.IsVisible = true;
            SplitBarBorder.IsVisible = true;

            _splitLeft = left;
            _splitRight = right;

            // Apply widths immediately if we already have measured size.
            UpdateSplitBarWidths();
        }

        if (saveToHistory)
        {
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

            if (!TryReadInputs(out var amount, out var rate, out var months))
            {
                RenderEmptyResult();
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
                RenderResult(result.FirstPayment, result.TotalPayment, result.Overpayment, amount, rate, months, LocalizationService.T("Differentiated"), isFirstPayment: true);
            }
        }
        catch (TaskCanceledException)
        {
            // expected during fast typing
        }
        catch
        {
            // Do not spam UI errors during live typing.
            RenderEmptyResult();
        }
    }

    private void RenderEmptyResult()
    {
        var settings = AppSettingsService.Get();
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
            return; // not measured yet
        }

        // Account for Grid column spacing.
        var inner = Math.Max(0, w - SplitBarGrid.ColumnSpacing);
        var leftW = inner * (_splitLeft / total);
        var rightW = Math.Max(0, inner - leftW);

        SplitBarAmountFill.WidthRequest = leftW;
        SplitBarOverpaymentFill.WidthRequest = rightW;
    }

    private bool TryReadInputs(out double amount, out double rate, out int months)
    {
        amount = 0;
        rate = 0;
        months = 0;

        var amountText = (AmountEntry.Text ?? string.Empty)
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

        if (!double.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out amount) ||
            !double.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out rate) ||
            !int.TryParse(monthsText, NumberStyles.Any, CultureInfo.InvariantCulture, out months))
        {
            return false;
        }

        if (amount <= 0 || rate < 0 || months <= 0)
        {
            return false;
        }

        return true;
    }

    private async void OnOpenAnalyticsClicked(object sender, EventArgs e)
    {
        try
        {
            string amountText = (AmountEntry.Text ?? string.Empty)
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

            if (!double.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ||
                !double.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
                !int.TryParse(monthsText, NumberStyles.Any, CultureInfo.InvariantCulture, out var months) ||
                amount <= 0 || rate < 0 || months <= 0)
            {
                await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
                return;
            }

            var paymentTypeKey = PaymentTypePicker.SelectedIndex == 0 ? "Annuity" : "Differentiated";

            // Save current snapshot into history.
            if (paymentTypeKey == "Annuity")
            {
                var result = LoanCalculator.CalculateAnnuity(amount, rate, months);
                CalculationHistoryService.Add(new Models.LoanHistoryItem
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    ProductType = LocalizationService.T("Credits"),
                    Amount = amount,
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
                var result = LoanCalculator.CalculateDifferentiated(amount, rate, months);
                CalculationHistoryService.Add(new Models.LoanHistoryItem
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    ProductType = LocalizationService.T("Credits"),
                    Amount = amount,
                    Rate = rate,
                    Months = months,
                    PaymentType = LocalizationService.T("Differentiated"),
                    MonthlyPayment = result.FirstPayment,
                    TotalPayment = result.TotalPayment,
                    Overpayment = result.Overpayment
                });
            }

            await Shell.Current.GoToAsync(
                $"{nameof(AnalyticsPage)}?mode=credit&amount={amount.ToString(CultureInfo.InvariantCulture)}&rate={rate.ToString(CultureInfo.InvariantCulture)}&months={months}&paymentType={paymentTypeKey}");
        }
        catch (Exception ex)
        {
            await DisplayAlert(LocalizationService.T("Error"), ex.Message, LocalizationService.T("Ok"));
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
        ResultHeaderLabel.Text = LocalizationService.T("Result");
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
        PresetPicker.Items.Add(LocalizationService.T("ConsumerPreset"));
        PresetPicker.Items.Add(LocalizationService.T("AutoPreset"));
        PresetPicker.Items.Add(LocalizationService.T("MortgageStandardPreset"));
        if (PresetPicker.SelectedIndex < 0)
        {
            PresetPicker.SelectedIndex = 0;
        }
    }
}