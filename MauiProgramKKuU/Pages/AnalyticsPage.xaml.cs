using MauiProgramKKuU.Services;
using MauiProgramKKuU.ViewModels;
using Microsoft.Maui.ApplicationModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace MauiProgramKKuU.Pages;

public partial class AnalyticsPage : ContentPage, IQueryAttributable
{
    private readonly AnalyticsViewModel _vm = new();
    private CancellationTokenSource? _loadCts;

    public AnalyticsPage()
    {
        InitializeComponent();
        BindingContext = _vm;

        // Update split bar widths whenever layout is measured.
        SplitBarRemainingGrid.SizeChanged += (_, __) => UpdateSplitBarsWidths();
        SplitBarCumulativeGrid.SizeChanged += (_, __) => UpdateSplitBarsWidths();
        SplitBarPaymentGrid.SizeChanged += (_, __) => UpdateSplitBarsWidths();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        _ = LoadAsync(query, _loadCts.Token);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        PageTitleLabel.Text = LocalizationService.T("Analytics");
    }

    private async Task LoadAsync(IDictionary<string, object> query, CancellationToken token)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;
            });

            // Give UI thread a tiny slice to show loader.
            await Task.Yield();

            var mode = GetString(query, "mode");
            if (string.IsNullOrWhiteSpace(mode))
            {
                // Navigation from menu: show empty placeholder state, do not build graphs.
                return;
            }

            var built = false;
            var modeIsKnown = !string.IsNullOrWhiteSpace(mode);

            if (mode.Equals("credit", StringComparison.OrdinalIgnoreCase))
            {
                if (TryGetDouble(query, "amount", out var amount) &&
                    TryGetDouble(query, "rate", out var rate) &&
                    TryGetInt(query, "months", out var months))
                {
                    var paymentType = GetString(query, "paymentType");
                    _vm.BuildForCredit(amount, rate, months, paymentType);
                    built = true;
                }
            }
            else if (mode.Equals("mortgage", StringComparison.OrdinalIgnoreCase))
            {
                if (TryGetDouble(query, "price", out var price) &&
                    TryGetDouble(query, "initial", out var initial) &&
                    TryGetDouble(query, "rate", out var rate) &&
                    TryGetInt(query, "months", out var months))
                {
                    var paymentType = GetString(query, "paymentType");
                    _vm.BuildForMortgage(price, initial, rate, months, paymentType);
                    built = true;
                }
            }
            else if (mode.Equals("compare", StringComparison.OrdinalIgnoreCase))
            {
                if (TryGetDouble(query, "amount", out var amount) &&
                    TryGetDouble(query, "rate", out var rate) &&
                    TryGetInt(query, "months", out var months))
                {
                    var exportScenarioRaw = GetString(query, "exportScenario");
                    var exportScenarioIndex = 1;
                    if (int.TryParse(exportScenarioRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    {
                        exportScenarioIndex = parsed;
                    }

                    _vm.BuildForCompare(amount, rate, months, exportScenarioIndex);
                    built = true;
                }
            }

            if (!token.IsCancellationRequested && (built || modeIsKnown))
            {
                RefreshBars();
            }
        }
        finally
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            });
        }
    }

    private void UpdateSplitBarsWidths()
    {
        // Remaining debt bar: remaining debt vs already paid interest.
        UpdateSplitBar(
            SplitBarRemainingGrid,
            SplitBarRemainingLeftFill,
            SplitBarRemainingRightFill,
            _vm.RemainingDebtSplitLeftValue,
            _vm.RemainingDebtSplitRightValue);

        // Cumulative principal vs interest.
        UpdateSplitBar(
            SplitBarCumulativeGrid,
            SplitBarCumulativeLeftFill,
            SplitBarCumulativeRightFill,
            _vm.CumulativePrincipalInterestSplitLeftValue,
            _vm.CumulativePrincipalInterestSplitRightValue);

        // Payment dynamics.
        UpdateSplitBar(
            SplitBarPaymentGrid,
            SplitBarPaymentLeftFill,
            SplitBarPaymentRightFill,
            _vm.PaymentDynamicsSplitLeftValue,
            _vm.PaymentDynamicsSplitRightValue);

        UpdateSplitBarsLabels();
    }

    private void UpdateSplitBarsLabels()
    {
        var settings = AppSettingsService.Get();
        var digits = settings.RoundingDigits;
        var currency = settings.CurrencySymbol;

        // Remaining debt.
        var remainingTotal = _vm.RemainingDebtSplitLeftValue + _vm.RemainingDebtSplitRightValue;
        if (remainingTotal <= 0)
        {
            RemainingDebtMonthLabel.Text = string.Empty;
            RemainingDebtLeftLabel.Text = "-";
            RemainingDebtRightLabel.Text = "-";
        }
        else
        {
            RemainingDebtMonthLabel.Text = $"на {_vm.RemainingDebtSplitMonthNumber} мес.";
            RemainingDebtLeftLabel.Text =
                $"Остаток тела: {Math.Round(_vm.RemainingDebtSplitLeftValue, digits):F2} {currency}";
            RemainingDebtRightLabel.Text =
                $"Остаток процентов: {Math.Round(_vm.RemainingDebtSplitRightValue, digits):F2} {currency}";
        }

        // Cumulative.
        var cumulativeTotal = _vm.CumulativePrincipalInterestSplitLeftValue + _vm.CumulativePrincipalInterestSplitRightValue;
        if (cumulativeTotal <= 0)
        {
            CumulativeLeftLabel.Text = "-";
            CumulativeRightLabel.Text = "-";
        }
        else
        {
            CumulativeLeftLabel.Text =
                $"Тело долга: {Math.Round(_vm.CumulativePrincipalInterestSplitLeftValue, digits):F2} {currency}";
            CumulativeRightLabel.Text =
                $"Проценты (всего): {Math.Round(_vm.CumulativePrincipalInterestSplitRightValue, digits):F2} {currency}";
        }

        // Payment dynamics.
        var paymentTotal = _vm.PaymentDynamicsSplitLeftValue + _vm.PaymentDynamicsSplitRightValue;
        if (paymentTotal <= 0)
        {
            PaymentDynamicsMonthLabel.Text = string.Empty;
            PaymentLeftLabel.Text = "-";
            PaymentRightLabel.Text = "-";
        }
        else
        {
            PaymentDynamicsMonthLabel.Text = $"разбивка за {_vm.PaymentDynamicsSplitMonthNumber} мес.";
            PaymentLeftLabel.Text =
                $"Тело: {Math.Round(_vm.PaymentDynamicsSplitLeftValue, digits):F2} {currency}";
            PaymentRightLabel.Text =
                $"Проценты: {Math.Round(_vm.PaymentDynamicsSplitRightValue, digits):F2} {currency}";
        }
    }

    private static void UpdateSplitBar(Grid grid, BoxView left, BoxView right, double leftValue, double rightValue)
    {
        if (grid is null)
        {
            return;
        }

        var total = Math.Max(0, leftValue) + Math.Max(0, rightValue);
        if (total <= 0)
        {
            left.WidthRequest = 0;
            right.WidthRequest = 0;
            return;
        }

        var w = grid.Width;
        if (w <= 0)
        {
            return;
        }

        var inner = Math.Max(0, w - grid.ColumnSpacing);
        var leftW = inner * (Math.Max(0, leftValue) / total);
        var rightW = Math.Max(0, inner - leftW);

        left.WidthRequest = leftW;
        right.WidthRequest = rightW;
    }

    private async void OnExportCsvClicked(object sender, EventArgs e)
    {
        try
        {
            var path = await CsvExportService.ExportScheduleAsync(_vm.CurrentSchedule, _vm.CurrentExportTitle);
            await DisplayAlert(LocalizationService.T("Done"), LocalizationService.T("CsvSaved") + $": {path}", LocalizationService.T("Ok"));
        }
        catch (Exception ex)
        {
            await DisplayAlert(LocalizationService.T("Error"), ex.Message, LocalizationService.T("Ok"));
        }
    }

    private async void OnExportPdfClicked(object sender, EventArgs e)
    {
        try
        {
            var path = await PdfExportService.ExportScheduleAsync(_vm.CurrentSchedule, _vm.CurrentExportTitle);
            await DisplayAlert(LocalizationService.T("Done"), LocalizationService.T("PdfSaved") + $": {path}", LocalizationService.T("Ok"));
        }
        catch (Exception ex)
        {
            await DisplayAlert(LocalizationService.T("Error"), ex.Message, LocalizationService.T("Ok"));
        }
    }

    private void RefreshBars()
    {
        UpdateSplitBarsWidths();
    }

    private void OnSelectExportScenario0Clicked(object sender, EventArgs e)
    {
        _vm.SetCompareExportScenarioIndex(0);
        RefreshBars();
    }

    private void OnSelectExportScenario1Clicked(object sender, EventArgs e)
    {
        _vm.SetCompareExportScenarioIndex(1);
        RefreshBars();
    }

    private void OnSelectExportScenario2Clicked(object sender, EventArgs e)
    {
        _vm.SetCompareExportScenarioIndex(2);
        RefreshBars();
    }

    private static string GetString(IDictionary<string, object> query, string key)
    {
        if (query.TryGetValue(key, out var value))
        {
            return value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool TryGetDouble(IDictionary<string, object> query, string key, out double value)
    {
        value = 0;
        if (!query.TryGetValue(key, out var raw))
        {
            return false;
        }

        var s = raw?.ToString() ?? string.Empty;
        s = s.Replace("%", string.Empty)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace(',', '.')
            .Trim();

        return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetInt(IDictionary<string, object> query, string key, out int value)
    {
        value = 0;
        if (!query.TryGetValue(key, out var raw))
        {
            return false;
        }

        var s = raw?.ToString() ?? string.Empty;
        s = s.Trim();
        return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }
}

