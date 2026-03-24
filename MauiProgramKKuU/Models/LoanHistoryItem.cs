namespace MauiProgramKKuU.Models;

public class LoanHistoryItem
{
    public DateTime CreatedAtUtc { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double Rate { get; set; }
    public int Months { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public double MonthlyPayment { get; set; }
    public double TotalPayment { get; set; }
    public double Overpayment { get; set; }
}
