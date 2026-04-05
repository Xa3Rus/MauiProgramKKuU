using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class ComparePage : ContentPage
{
    public ComparePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = LocalizationService.T("Compare");
        CompareTitleLabel.Text = LocalizationService.T("CompareThreeScenarios");
        AmountEntry.Placeholder = LocalizationService.T("Amount");
        RateEntry.Placeholder = LocalizationService.T("RatePercent");
        MonthsEntry.Placeholder = LocalizationService.T("BaseTermMonths");
        CompareButton.Text = LocalizationService.T("CompareButton");
    }

    private async void OnCompareClicked(object sender, EventArgs e)
    {
        if (!double.TryParse((AmountEntry.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var amount) ||
            !double.TryParse((RateEntry.Text ?? string.Empty).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var rate) ||
            !int.TryParse(MonthsEntry.Text, out var months) ||
            amount <= 0 || rate < 0 || months <= 0)
        {
            await DisplayAlert(LocalizationService.T("Error"), LocalizationService.T("InvalidData"), LocalizationService.T("Ok"));
            return;
        }

        // Open the shared analytics view (3 scenarios overlaid).
        await Shell.Current.GoToAsync(
            $"{nameof(AnalyticsPage)}?mode=compare&amount={amount.ToString(CultureInfo.InvariantCulture)}&rate={rate.ToString(CultureInfo.InvariantCulture)}&months={months}&exportScenario=1");
    }
}
