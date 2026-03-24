using MauiProgramKKuU.Pages;

namespace MauiProgramKKuU;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(CreditPage), typeof(CreditPage));
        Routing.RegisterRoute(nameof(MortgagePage), typeof(MortgagePage));
    }
}