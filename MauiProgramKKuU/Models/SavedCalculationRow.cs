namespace MauiProgramKKuU.Models;

public sealed class SavedCalculationRow
{
    public string ProductType { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double Rate { get; set; }
    public int Months { get; set; }

    public string CreatedAtLocalText { get; set; } = string.Empty;
    public string MainLine { get; set; } = string.Empty;
    public string TotalLine { get; set; } = string.Empty;
}

