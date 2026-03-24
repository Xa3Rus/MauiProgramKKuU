namespace MauiProgramKKuU.Models;

public class PaymentScheduleItem
{
    public int MonthNumber { get; set; }
    public double Payment { get; set; }
    public double Principal { get; set; }
    public double Interest { get; set; }
    public double RemainingDebt { get; set; }
}
