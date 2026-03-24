using MauiProgramKKuU.Pages;

namespace MauiProgramKKuU;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(CreditPage), typeof(CreditPage));
        Routing.RegisterRoute(nameof(MortgagePage), typeof(MortgagePage));
        Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
        Routing.RegisterRoute(nameof(ComparePage), typeof(ComparePage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(FaqPage), typeof(FaqPage));
    }
}