using MauiProgramKKuU.Models;
using MauiProgramKKuU.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiProgramKKuU.ViewModels;

public sealed class AnalyticsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private const int MaxTermMonths = 1200; // guard: avoid UI freezes on extremely large input

    private string _productTitle = string.Empty;
    public string ProductTitle
    {
        get => _productTitle;
        private set => SetProperty(ref _productTitle, value);
    }

    private string _loanAmountText = "-";
    public string LoanAmountText
    {
        get => _loanAmountText;
        private set => SetProperty(ref _loanAmountText, value);
    }

    private string _monthlyPaymentText = "-";
    public string MonthlyPaymentText
    {
        get => _monthlyPaymentText;
        private set => SetProperty(ref _monthlyPaymentText, value);
    }

    private string _totalPaymentText = "-";
    public string TotalPaymentText
    {
        get => _totalPaymentText;
        private set => SetProperty(ref _totalPaymentText, value);
    }

    private string _overpaymentText = "-";
    public string OverpaymentText
    {
        get => _overpaymentText;
        private set => SetProperty(ref _overpaymentText, value);
    }

    public PlotModel RemainingDebtModel { get; private set; } = new();
    public PlotModel CumulativePrincipalInterestModel { get; private set; } = new();
    public PlotModel PaymentDynamicsModel { get; private set; } = new();

    // Split bar values used instead of charts in AnalyticsPage.
    // Remaining debt: remaining debt vs already paid interest (at mid term).
    public double RemainingDebtSplitLeftValue { get; private set; }
    public double RemainingDebtSplitRightValue { get; private set; }
    // Cumulative: cumulative principal vs cumulative interest (end term).
    public double CumulativePrincipalInterestSplitLeftValue { get; private set; }
    public double CumulativePrincipalInterestSplitRightValue { get; private set; }
    // Payment dynamics: principal part vs interest part for a mid-term payment.
    public double PaymentDynamicsSplitLeftValue { get; private set; }
    public double PaymentDynamicsSplitRightValue { get; private set; }

    public int RemainingDebtSplitMonthNumber { get; private set; }
    public int PaymentDynamicsSplitMonthNumber { get; private set; }

    private List<PaymentScheduleItem> _currentSchedule = [];
    public IReadOnlyList<PaymentScheduleItem> CurrentSchedule => _currentSchedule;

    private string _currentExportTitle = string.Empty;
    public string CurrentExportTitle
    {
        get => _currentExportTitle;
        private set => SetProperty(ref _currentExportTitle, value);
    }

    private bool _isCompare;
    public bool IsCompare
    {
        get => _isCompare;
        private set => SetProperty(ref _isCompare, value);
    }

    private int _compareSelectedScenarioIndex = 1;
    public int CompareSelectedScenarioIndex
    {
        get => _compareSelectedScenarioIndex;
        private set => SetProperty(ref _compareSelectedScenarioIndex, value);
    }

    private string _scenario1Text = string.Empty;
    public string Scenario1Text
    {
        get => _scenario1Text;
        private set => SetProperty(ref _scenario1Text, value);
    }

    private string _scenario2Text = string.Empty;
    public string Scenario2Text
    {
        get => _scenario2Text;
        private set => SetProperty(ref _scenario2Text, value);
    }

    private string _scenario3Text = string.Empty;
    public string Scenario3Text
    {
        get => _scenario3Text;
        private set => SetProperty(ref _scenario3Text, value);
    }

    private readonly List<List<PaymentScheduleItem>> _compareSchedules = [];
    private readonly List<int> _compareTerms = [];

    private int _compareMaxTerm = 0;
    private double _compareAmount = 0;
    private double _compareAnnualRatePercent = 0;

    public void BuildForCredit(double amount, double annualRatePercent, int months, string paymentTypeKey)
    {
        IsCompare = false;

        months = Math.Clamp(months, 1, MaxTermMonths);

        var settings = AppSettingsService.Get();
        var paymentType = NormalizePaymentTypeKey(paymentTypeKey);

        ProductTitle = LocalizationService.T("Credits");
        LoanAmountText = $"{LocalizationService.T("LoanAmount")}: {FormatMoney(amount, settings)}";
        CurrentExportTitle = $"{LocalizationService.T("Credits")} {amount:F2}_{annualRatePercent:F2}";

        double monthlyPayment;
        double totalPayment;
        double overpayment;

        if (paymentType == PaymentTypeKey.Annuity)
        {
            var calc = LoanCalculator.CalculateAnnuity(amount, annualRatePercent, months);
            monthlyPayment = calc.MonthlyPayment;
            totalPayment = calc.TotalPayment;
            overpayment = calc.Overpayment;
        }
        else
        {
            var calc = LoanCalculator.CalculateDifferentiated(amount, annualRatePercent, months);
            monthlyPayment = calc.FirstPayment; // For differentiated we show the first month payment (as in current UI).
            totalPayment = calc.TotalPayment;
            overpayment = calc.Overpayment;
        }

        MonthlyPaymentText = BuildMonthlyPaymentText(monthlyPayment, paymentType, settings);
        TotalPaymentText = $"{LocalizationService.T("TotalPayment")}: {FormatMoney(totalPayment, settings)}";
        OverpaymentText = $"{LocalizationService.T("Overpayment")}: {FormatMoney(overpayment, settings)}";

        var schedule = paymentType == PaymentTypeKey.Annuity
            ? LoanCalculator.BuildAnnuitySchedule(amount, annualRatePercent, months)
            : LoanCalculator.BuildDifferentiatedSchedule(amount, annualRatePercent, months);

        BuildCharts(schedule, months, settings);
    }

    public void BuildForMortgage(double propertyPrice, double initialPayment, double annualRatePercent, int months, string paymentTypeKey)
    {
        IsCompare = false;

        months = Math.Clamp(months, 1, MaxTermMonths);

        var settings = AppSettingsService.Get();
        var paymentType = NormalizePaymentTypeKey(paymentTypeKey);

        var loanAmount = propertyPrice - initialPayment;
        if (loanAmount <= 0)
        {
            ProductTitle = LocalizationService.T("Mortgage");
            LoanAmountText = $"{LocalizationService.T("MortgageAmount")}: -";
            MonthlyPaymentText = "-";
            TotalPaymentText = "-";
            OverpaymentText = "-";
            return;
        }

        ProductTitle = LocalizationService.T("Mortgage");
        LoanAmountText = $"{LocalizationService.T("MortgageAmount")}: {FormatMoney(loanAmount, settings)}";
        CurrentExportTitle = $"{LocalizationService.T("Mortgage")} {loanAmount:F2}_{annualRatePercent:F2}";

        double monthlyPayment;
        double totalPayment;
        double overpayment;

        if (paymentType == PaymentTypeKey.Annuity)
        {
            var calc = LoanCalculator.CalculateAnnuity(loanAmount, annualRatePercent, months);
            monthlyPayment = calc.MonthlyPayment;
            totalPayment = calc.TotalPayment;
            overpayment = calc.Overpayment;
        }
        else
        {
            var calc = LoanCalculator.CalculateDifferentiated(loanAmount, annualRatePercent, months);
            monthlyPayment = calc.FirstPayment;
            totalPayment = calc.TotalPayment;
            overpayment = calc.Overpayment;
        }

        MonthlyPaymentText = BuildMonthlyPaymentText(monthlyPayment, paymentType, settings);
        TotalPaymentText = $"{LocalizationService.T("TotalPayment")}: {FormatMoney(totalPayment, settings)}";
        OverpaymentText = $"{LocalizationService.T("Overpayment")}: {FormatMoney(overpayment, settings)}";

        var schedule = paymentType == PaymentTypeKey.Annuity
            ? LoanCalculator.BuildAnnuitySchedule(loanAmount, annualRatePercent, months)
            : LoanCalculator.BuildDifferentiatedSchedule(loanAmount, annualRatePercent, months);

        BuildCharts(schedule, months, settings);
    }

    public void BuildForCompare(double amount, double annualRatePercent, int baseMonths, int selectedScenarioIndex)
    {
        IsCompare = true;

        var settings = AppSettingsService.Get();

        baseMonths = Math.Clamp(baseMonths, 1, MaxTermMonths);

        _compareSchedules.Clear();
        _compareTerms.Clear();
        _compareAmount = amount;
        _compareAnnualRatePercent = annualRatePercent;

        var t1 = Math.Max(1, baseMonths - 12);
        var t2 = Math.Max(1, baseMonths);
        var t3 = Math.Max(1, baseMonths + 12);

        _compareTerms.Add(t1);
        _compareTerms.Add(t2);
        _compareTerms.Add(t3);
        _compareMaxTerm = Math.Max(t1, Math.Max(t2, t3));

        var r1 = LoanCalculator.CalculateAnnuity(amount, annualRatePercent, t1);
        var r2 = LoanCalculator.CalculateAnnuity(amount, annualRatePercent, t2);
        var r3 = LoanCalculator.CalculateAnnuity(amount, annualRatePercent, t3);

        Scenario1Text = $"{t1} {LocalizationService.T("MonthsShort")}: {r1.MonthlyPayment:F2} {settings.CurrencySymbol}/{LocalizationService.T("MonthShort")} | {LocalizationService.T("Overpayment").ToLowerInvariant()} {r1.Overpayment:F2} {settings.CurrencySymbol}";
        Scenario2Text = $"{t2} {LocalizationService.T("MonthsShort")}: {r2.MonthlyPayment:F2} {settings.CurrencySymbol}/{LocalizationService.T("MonthShort")} | {LocalizationService.T("Overpayment").ToLowerInvariant()} {r2.Overpayment:F2} {settings.CurrencySymbol}";
        Scenario3Text = $"{t3} {LocalizationService.T("MonthsShort")}: {r3.MonthlyPayment:F2} {settings.CurrencySymbol}/{LocalizationService.T("MonthShort")} | {LocalizationService.T("Overpayment").ToLowerInvariant()} {r3.Overpayment:F2} {settings.CurrencySymbol}";

        _compareSchedules.Add(LoanCalculator.BuildAnnuitySchedule(amount, annualRatePercent, t1));
        _compareSchedules.Add(LoanCalculator.BuildAnnuitySchedule(amount, annualRatePercent, t2));
        _compareSchedules.Add(LoanCalculator.BuildAnnuitySchedule(amount, annualRatePercent, t3));

        var selected = Math.Clamp(selectedScenarioIndex, 0, 2);
        CompareSelectedScenarioIndex = selected;

        CurrentExportTitle = $"{LocalizationService.T("Compare")}_{amount:F0}_{annualRatePercent:F2}_{_compareTerms[selected]}";

        // Build selected-scenario charts (the other two remain overlay-neutral).
        BuildCharts(_compareSchedules[selected], _compareTerms[selected], settings);

        // Overlay remaining debt for all 3 scenarios.
        BuildCompareRemainingDebtOverlay();
    }

    public void SetCompareExportScenarioIndex(int scenarioIndex)
    {
        if (!IsCompare)
        {
            return;
        }

        if (_compareSchedules.Count != 3 || _compareTerms.Count != 3)
        {
            return;
        }

        var selected = Math.Clamp(scenarioIndex, 0, 2);
        if (selected == CompareSelectedScenarioIndex)
        {
            return;
        }

        CompareSelectedScenarioIndex = selected;

        var settings = AppSettingsService.Get();
        CurrentExportTitle = $"{LocalizationService.T("Compare")}_{_compareAmount:F0}_{_compareAnnualRatePercent:F2}_{_compareTerms[selected]}";

        // Rebuild selected scenario charts.
        BuildCharts(_compareSchedules[selected], _compareTerms[selected], settings);

        // Restore overlay for remaining debt.
        BuildCompareRemainingDebtOverlay();
    }

    private void BuildCharts(List<PaymentScheduleItem> schedule, int months, AppSettings settings)
    {
        _currentSchedule = schedule;

        // Compute split-bar values (used instead of PlotViews on AnalyticsPage).
        if (schedule.Count == 0)
        {
            RemainingDebtSplitLeftValue = 0;
            RemainingDebtSplitRightValue = 0;
            CumulativePrincipalInterestSplitLeftValue = 0;
            CumulativePrincipalInterestSplitRightValue = 0;
            PaymentDynamicsSplitLeftValue = 0;
            PaymentDynamicsSplitRightValue = 0;
            RemainingDebtSplitMonthNumber = 0;
            PaymentDynamicsSplitMonthNumber = 0;
        }
        else
        {
            var midIndex = Math.Clamp(schedule.Count / 2, 0, schedule.Count - 1);
            var midItem = schedule[midIndex];

            double cumPrincipalEnd = 0;
            double cumInterestEnd = 0;
            double cumInterestMid = 0;

            for (int i = 0; i < schedule.Count; i++)
            {
                cumPrincipalEnd += schedule[i].Principal;
                cumInterestEnd += schedule[i].Interest;

                if (i == midIndex)
                {
                    // Cumulated interest up to and including midIndex.
                    cumInterestMid = cumInterestEnd;
                }
            }

            // Remaining debt bar: remaining principal (body) vs remaining interest till end.
            var interestRemainingMid = cumInterestEnd - cumInterestMid;
            RemainingDebtSplitLeftValue = midItem.RemainingDebt;
            RemainingDebtSplitRightValue = interestRemainingMid;
            RemainingDebtSplitMonthNumber = midItem.MonthNumber;

            CumulativePrincipalInterestSplitLeftValue = cumPrincipalEnd;
            CumulativePrincipalInterestSplitRightValue = cumInterestEnd;

            PaymentDynamicsSplitLeftValue = midItem.Principal;
            PaymentDynamicsSplitRightValue = midItem.Interest;
            PaymentDynamicsSplitMonthNumber = midItem.MonthNumber;
        }

        var accent = OxyThemeColors.Accent;
        var text = OxyThemeColors.Text;
        var subtext = OxyThemeColors.Subtext;
        var divider = OxyThemeColors.Divider;

        // Downsample: keep charts responsive on long schedules.
        var maxPoints = 240;
        var step = Math.Max(1, months / maxPoints);

        var remainingSeries = new LineSeries
        {
            Title = "Remaining debt",
            Color = accent,
            StrokeThickness = 2,
            LineStyle = LineStyle.Solid,
            MarkerType = MarkerType.None
        };

        var paymentSeries = new LineSeries
        {
            Title = "Payment",
            Color = subtext,
            StrokeThickness = 2,
            LineStyle = LineStyle.Solid,
            MarkerType = MarkerType.None
        };

        var cumulativePrincipalSeries = new LineSeries
        {
            Title = "Principal (cum.)",
            Color = OxyThemeColors.Success,
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };

        var cumulativeInterestSeries = new LineSeries
        {
            Title = "Interest (cum.)",
            Color = OxyThemeColors.Warning,
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };

        double cumulativePrincipal = 0;
        double cumulativeInterest = 0;

        for (int i = 0; i < schedule.Count; i++)
        {
            if (i % step != 0 && i != schedule.Count - 1)
            {
                continue;
            }

            var item = schedule[i];
            remainingSeries.Points.Add(new DataPoint(item.MonthNumber, item.RemainingDebt));
            paymentSeries.Points.Add(new DataPoint(item.MonthNumber, item.Payment));

            cumulativePrincipal += item.Principal;
            cumulativeInterest += item.Interest;

            cumulativePrincipalSeries.Points.Add(new DataPoint(item.MonthNumber, cumulativePrincipal));
            cumulativeInterestSeries.Points.Add(new DataPoint(item.MonthNumber, cumulativeInterest));
        }

        RemainingDebtModel = CreateMoneyLineModel(
            title: LocalizationService.T("RemainingDebt"),
            subtitle: string.Empty,
            xMin: 1,
            xMax: months,
            accentSeries: remainingSeries,
            text: text,
            divider: divider,
            clampYToZero: true,
            showLegend: false);

        CumulativePrincipalInterestModel = CreateMoneyLineModel(
            title: LocalizationService.T("PrincipalVsInterest"),
            subtitle: string.Empty,
            xMin: 1,
            xMax: months,
            accentSeries: cumulativePrincipalSeries,
            text: text,
            divider: divider,
            additionalSeries: [cumulativeInterestSeries],
            clampYToZero: true,
            showLegend: false);

        PaymentDynamicsModel = CreateMoneyLineModel(
            title: LocalizationService.T("PaymentDynamics"),
            subtitle: string.Empty,
            xMin: 1,
            xMax: months,
            accentSeries: paymentSeries,
            text: text,
            divider: divider,
            clampYToZero: false,
            showLegend: false,
            yLabelFormat: "0.##");
    }

    private void BuildCompareRemainingDebtOverlay()
    {
        if (_compareSchedules.Count != 3 || _compareTerms.Count != 3)
        {
            return;
        }

        var text = OxyThemeColors.Text;
        var divider = OxyThemeColors.Divider;

        var accent1 = new LineSeries
        {
            Title = "S1",
            Color = OxyThemeColors.Accent,
            StrokeThickness = 2.5,
            LineStyle = LineStyle.Solid,
            MarkerType = MarkerType.None
        };
        var accent2 = new LineSeries
        {
            Title = "S2",
            Color = OxyThemeColors.Success,
            StrokeThickness = 2.5,
            LineStyle = LineStyle.Solid,
            MarkerType = MarkerType.None
        };
        var accent3 = new LineSeries
        {
            Title = "S3",
            Color = OxyThemeColors.Warning,
            StrokeThickness = 2.5,
            LineStyle = LineStyle.Solid,
            MarkerType = MarkerType.None
        };

        // Downsample relative to max term for performance.
        var maxPoints = 240;
        var step = Math.Max(1, _compareMaxTerm / maxPoints);

        for (var i = 0; i < _compareSchedules[0].Count; i++)
        {
            if (i % step != 0 && i != _compareSchedules[0].Count - 1)
            {
                continue;
            }

            var item = _compareSchedules[0][i];
            accent1.Points.Add(new DataPoint(item.MonthNumber, item.RemainingDebt));
        }

        for (var i = 0; i < _compareSchedules[1].Count; i++)
        {
            if (i % step != 0 && i != _compareSchedules[1].Count - 1)
            {
                continue;
            }

            var item = _compareSchedules[1][i];
            accent2.Points.Add(new DataPoint(item.MonthNumber, item.RemainingDebt));
        }

        for (var i = 0; i < _compareSchedules[2].Count; i++)
        {
            if (i % step != 0 && i != _compareSchedules[2].Count - 1)
            {
                continue;
            }

            var item = _compareSchedules[2][i];
            accent3.Points.Add(new DataPoint(item.MonthNumber, item.RemainingDebt));
        }

        RemainingDebtModel = CreateMoneyLineModel(
            title: LocalizationService.T("RemainingDebt"),
            subtitle: string.Empty,
            xMin: 1,
            xMax: _compareMaxTerm,
            accentSeries: accent1,
            text: text,
            divider: divider,
            additionalSeries: [accent2, accent3],
            clampYToZero: true,
            showLegend: false);
    }

    private static PlotModel CreateMoneyLineModel(
        string title,
        string subtitle,
        double xMin,
        double xMax,
        LineSeries accentSeries,
        OxyColor text,
        OxyColor divider,
        IReadOnlyList<OxyPlot.Series.Series>? additionalSeries = null,
        bool clampYToZero = true,
        bool showLegend = false,
        int desiredXTicks = 6,
        string yLabelFormat = "0")
    {
        // Note: PlotModel is created as immutable UI state.
        var model = new PlotModel
        {
            Title = title,
            Subtitle = subtitle,
            Background = OxyColors.Transparent,
            TextColor = text,
            SubtitleColor = text,
            IsLegendVisible = showLegend,
            PlotAreaBorderColor = OxyColors.Transparent
        };

        model.TitleFontSize = 14;

        var allSeries = new List<OxyPlot.Series.Series> { accentSeries };
        if (additionalSeries is not null)
        {
            allSeries.AddRange(additionalSeries);
        }

        var (minY, maxY) = GetCombinedMinMaxY(allSeries);
        var (minX, maxX) = GetCombinedMinMaxX(allSeries);

        var spanY = Math.Max(1e-9, maxY - minY);
        var paddingY = spanY * 0.08;

        var effectiveMinY = clampYToZero ? Math.Max(0, minY - paddingY) : minY - paddingY;
        var effectiveMaxY = maxY + paddingY;

        // Ensure tick values are human-friendly.
        var xSpan = Math.Max(1, (int)Math.Round(maxX - minX));
        var tickCount = Math.Max(2, desiredXTicks);
        var rawStep = xSpan / (double)(tickCount - 1);
        var majorStep = Math.Max(1, (int)Math.Round(rawStep));
        var majorStepSafe = Math.Min(majorStep, Math.Max(1, xSpan)); // avoid runaway

        var gridColor = WithAlpha(divider, 90);

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = xMin,
            Maximum = xMax,
            MajorGridlineColor = gridColor,
            MinorGridlineColor = gridColor,
            TextColor = text,
            TicklineColor = divider,
            AxislineColor = divider,
            MajorStep = majorStepSafe,
            MinorGridlineStyle = LineStyle.None,
            StringFormat = "0",
            FontSize = 11
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = effectiveMinY,
            Maximum = effectiveMaxY,
            MajorGridlineColor = gridColor,
            MinorGridlineColor = gridColor,
            TextColor = text,
            TicklineColor = divider,
            AxislineColor = divider,
            MinorGridlineStyle = LineStyle.None,
            StringFormat = yLabelFormat,
            FontSize = 11
        });

        model.Series.Add(accentSeries);
        if (additionalSeries is not null)
        {
            foreach (var s in additionalSeries)
            {
                model.Series.Add(s);
            }
        }

        return model;
    }

    private static (double minY, double maxY) GetCombinedMinMaxY(IEnumerable<OxyPlot.Series.Series> series)
    {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;

        foreach (var s in series)
        {
            if (s is LineSeries ls)
            {
                foreach (var p in ls.Points)
                {
                    min = Math.Min(min, p.Y);
                    max = Math.Max(max, p.Y);
                }
            }
        }

        if (double.IsInfinity(min) || double.IsInfinity(max))
        {
            min = 0;
            max = 1;
        }

        return (min, max);
    }

    private static (double minX, double maxX) GetCombinedMinMaxX(IEnumerable<OxyPlot.Series.Series> series)
    {
        var min = double.PositiveInfinity;
        var max = double.NegativeInfinity;

        foreach (var s in series)
        {
            if (s is LineSeries ls)
            {
                foreach (var p in ls.Points)
                {
                    min = Math.Min(min, p.X);
                    max = Math.Max(max, p.X);
                }
            }
        }

        if (double.IsInfinity(min) || double.IsInfinity(max))
        {
            min = 0;
            max = 1;
        }

        return (min, max);
    }

    private static OxyColor WithAlpha(OxyColor color, byte alpha)
        => OxyColor.FromArgb(alpha, color.R, color.G, color.B);

    private static string BuildMonthlyPaymentText(double payment, PaymentTypeKey paymentType, AppSettings settings)
    {
        var digits = settings.RoundingDigits;
        var prefix = paymentType == PaymentTypeKey.Annuity ? LocalizationService.T("MonthlyPayment") : LocalizationService.T("FirstPayment");
        return $"{prefix}: {Math.Round(payment, digits):F2} {settings.CurrencySymbol}";
    }

    private string FormatMoney(double value, AppSettings settings)
    {
        var digits = settings.RoundingDigits;
        return $"{Math.Round(value, digits):F2} {settings.CurrencySymbol}";
    }

    private enum PaymentTypeKey
    {
        Annuity,
        Differentiated
    }

    private static PaymentTypeKey NormalizePaymentTypeKey(string paymentTypeKey)
    {
        var value = (paymentTypeKey ?? string.Empty).Trim();
        var lower = value.ToLowerInvariant();

        if (lower.Contains("differentiated", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("дифференц", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentTypeKey.Differentiated;
        }

        if (lower.Contains("annuity", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("аннуитет", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentTypeKey.Annuity;
        }

        // Fallback to annuity.
        return PaymentTypeKey.Annuity;
    }

    private static class OxyThemeColors
    {
        public static OxyColor Accent => FromResource("AccentColor", OxyColors.SkyBlue);
        public static OxyColor Text => FromResource("AppTextColor", OxyColors.White);
        public static OxyColor Subtext => FromResource("AppSubtextColor", OxyColors.LightGray);
        public static OxyColor Divider => FromResource("DividerColor", OxyColor.Parse("#2A334C"));
        public static OxyColor Success => FromResource("SuccessColor", OxyColors.SeaGreen);
        public static OxyColor Warning => FromResource("WarningColor", OxyColors.OrangeRed);

        private static OxyColor FromResource(string key, OxyColor fallback)
        {
            try
            {
                var resources = Application.Current?.Resources;
                if (resources is null) return fallback;
                if (!resources.TryGetValue(key, out var value)) return fallback;
                return value is Microsoft.Maui.Graphics.Color c
                    ? OxyColor.FromArgb((byte)(c.Alpha * 255), (byte)(c.Red * 255), (byte)(c.Green * 255), (byte)(c.Blue * 255))
                    : fallback;
            }
            catch
            {
                return fallback;
            }
        }
    }

    private void SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

internal static class SeriesExtensions
{
    // Marker extension to keep compilation happy when additional series are provided.
    public static IReadOnlyList<OxyPlot.Series.Series> AsSeriesList(this OxyPlot.Series.Series s) => [s];
}

