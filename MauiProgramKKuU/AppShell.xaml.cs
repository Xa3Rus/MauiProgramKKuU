using MauiProgramKKuU.Pages;
using MauiProgramKKuU.Services;

namespace MauiProgramKKuU;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(CreditPage), typeof(CreditPage));
        Routing.RegisterRoute(nameof(MortgagePage), typeof(MortgagePage));
        Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
        Routing.RegisterRoute(nameof(AnalyticsPage), typeof(AnalyticsPage));
        Routing.RegisterRoute(nameof(ComparePage), typeof(ComparePage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(FaqPage), typeof(FaqPage));

        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        HomeFlyout.Title = LocalizationService.T("Home");
        HomeShellContent.Title = LocalizationService.T("Home");

        HistoryFlyout.Title = LocalizationService.T("History");
        HistoryShellContent.Title = LocalizationService.T("History");

        AnalyticsFlyout.Title = LocalizationService.T("Analytics");
        AnalyticsShellContent.Title = LocalizationService.T("Analytics");

        SettingsFlyout.Title = LocalizationService.T("Settings");
        SettingsShellContent.Title = LocalizationService.T("Settings");

        FaqFlyout.Title = "FAQ";
        FaqShellContent.Title = "FAQ";
    }
}