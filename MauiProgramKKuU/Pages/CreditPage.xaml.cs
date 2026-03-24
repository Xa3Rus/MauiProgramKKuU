using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class CreditPage : ContentPage
{
    public CreditPage()
    {
        InitializeComponent();
        PaymentTypePicker.SelectedIndex = 0;
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        try
        {
            string amountText = AmountEntry.Text?.Replace(',', '.');
            string rateText = RateEntry.Text?.Replace(',', '.');
            string monthsText = MonthsEntry.Text;

            if (!double.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out double amount))
            {
                await DisplayAlert("Ошибка", "Введите корректную сумму кредита", "OK");
                return;
            }

            if (!double.TryParse(rateText, NumberStyles.Any, CultureInfo.InvariantCulture, out double rate))
            {
                await DisplayAlert("Ошибка", "Введите корректную процентную ставку", "OK");
                return;
            }

            if (!int.TryParse(monthsText, out int months))
            {
                await DisplayAlert("Ошибка", "Введите корректный срок", "OK");
                return;
            }

            if (amount <= 0 || rate < 0 || months <= 0)
            {
                await DisplayAlert("Ошибка", "Проверьте введённые данные", "OK");
                return;
            }

            if (PaymentTypePicker.SelectedIndex == 0)
            {
                var result = LoanCalculator.CalculateAnnuity(amount, rate, months);
                MonthlyPaymentLabel.Text = $"Ежемесячный платеж: {result.MonthlyPayment:F2} Br";
                TotalPaymentLabel.Text = $"Общая выплата: {result.TotalPayment:F2} Br";
                OverpaymentLabel.Text = $"Переплата: {result.Overpayment:F2} Br";
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(amount, rate, months);
                MonthlyPaymentLabel.Text = $"Первый платеж: {result.FirstPayment:F2} Br";
                TotalPaymentLabel.Text = $"Общая выплата: {result.TotalPayment:F2} Br";
                OverpaymentLabel.Text = $"Переплата: {result.Overpayment:F2} Br";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", ex.Message, "OK");
        }
    }
}