using MauiProgramKKuU.Services;
using System.Globalization;

namespace MauiProgramKKuU.Pages;

public partial class MortgagePage : ContentPage
{
    public MortgagePage()
    {
        InitializeComponent();
        PaymentTypePicker.SelectedIndex = 0;
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        try
        {
            string priceText = PropertyPriceEntry.Text?.Replace(',', '.');
            string initialText = InitialPaymentEntry.Text?.Replace(',', '.');
            string rateText = RateEntry.Text?.Replace(',', '.');
            string monthsText = MonthsEntry.Text;

            if (!double.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out double propertyPrice))
            {
                await DisplayAlert("Ошибка", "Введите корректную стоимость недвижимости", "OK");
                return;
            }

            if (!double.TryParse(initialText, NumberStyles.Any, CultureInfo.InvariantCulture, out double initialPayment))
            {
                await DisplayAlert("Ошибка", "Введите корректный первоначальный взнос", "OK");
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

            double loanAmount = propertyPrice - initialPayment;

            if (propertyPrice <= 0 || initialPayment < 0 || rate < 0 || months <= 0 || loanAmount <= 0)
            {
                await DisplayAlert("Ошибка", "Проверьте введённые данные", "OK");
                return;
            }

            LoanAmountLabel.Text = $"Сумма ипотеки: {loanAmount:F2} ₽";

            if (PaymentTypePicker.SelectedIndex == 0)
            {
                var result = LoanCalculator.CalculateAnnuity(loanAmount, rate, months);
                MonthlyPaymentLabel.Text = $"Ежемесячный платеж: {result.MonthlyPayment:F2} Br";
                TotalPaymentLabel.Text = $"Общая выплата: {result.TotalPayment:F2} Br";
                OverpaymentLabel.Text = $"Переплата: {result.Overpayment:F2} Br";
            }
            else
            {
                var result = LoanCalculator.CalculateDifferentiated(loanAmount, rate, months);
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