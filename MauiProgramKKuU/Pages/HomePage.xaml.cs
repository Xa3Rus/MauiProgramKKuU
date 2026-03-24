namespace MauiProgramKKuU.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnCreditTapped(object sender, TappedEventArgs e)
    {
        await AnimateCard(CreditCard);
        await Shell.Current.GoToAsync(nameof(CreditPage));
    }

    private async void OnMortgageTapped(object sender, TappedEventArgs e)
    {
        await AnimateCard(MortgageCard);
        await Shell.Current.GoToAsync(nameof(MortgagePage));
    }

    private async Task AnimateCard(VisualElement card)
    {
        await card.ScaleTo(0.97, 80);
        await card.ScaleTo(1.0, 80);
    }
}