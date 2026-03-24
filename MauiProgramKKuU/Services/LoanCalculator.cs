using MauiProgramKKuU.Models;

namespace MauiProgramKKuU.Services
{
    public static class LoanCalculator
    {
        public static (double MonthlyPayment, double TotalPayment, double Overpayment) CalculateAnnuity(double amount, double annualRate, int months)
        {
            double monthlyRate = annualRate / 12 / 100;
            double monthlyPayment;

            if (monthlyRate == 0)
            {
                monthlyPayment = amount / months;
            }
            else
            {
                double pow = Math.Pow(1 + monthlyRate, months);
                monthlyPayment = amount * (monthlyRate * pow) / (pow - 1);
            }

            double totalPayment = monthlyPayment * months;
            double overpayment = totalPayment - amount;

            return (monthlyPayment, totalPayment, overpayment);
        }

        public static (double FirstPayment, double TotalPayment, double Overpayment) CalculateDifferentiated(double amount, double annualRate, int months)
        {
            double monthlyRate = annualRate / 12 / 100;
            double principalPart = amount / months;
            double totalPayment = 0;
            double firstPayment = 0;

            for (int i = 0; i < months; i++)
            {
                double remainingDebt = amount - (principalPart * i);
                double interest = remainingDebt * monthlyRate;
                double payment = principalPart + interest;

                if (i == 0)
                    firstPayment = payment;

                totalPayment += payment;
            }

            double overpayment = totalPayment - amount;

            return (firstPayment, totalPayment, overpayment);
        }

        public static List<PaymentScheduleItem> BuildAnnuitySchedule(double amount, double annualRate, int months)
        {
            var schedule = new List<PaymentScheduleItem>();
            var monthlyRate = annualRate / 12 / 100;
            var monthlyPayment = CalculateAnnuity(amount, annualRate, months).MonthlyPayment;
            var debt = amount;

            for (int i = 1; i <= months; i++)
            {
                var interest = debt * monthlyRate;
                var principal = monthlyPayment - interest;
                debt -= principal;
                if (debt < 0)
                {
                    debt = 0;
                }

                schedule.Add(new PaymentScheduleItem
                {
                    MonthNumber = i,
                    Payment = monthlyPayment,
                    Principal = principal,
                    Interest = interest,
                    RemainingDebt = debt
                });
            }

            return schedule;
        }

        public static List<PaymentScheduleItem> BuildDifferentiatedSchedule(double amount, double annualRate, int months)
        {
            var schedule = new List<PaymentScheduleItem>();
            var monthlyRate = annualRate / 12 / 100;
            var principalPart = amount / months;
            var debt = amount;

            for (int i = 1; i <= months; i++)
            {
                var interest = debt * monthlyRate;
                var payment = principalPart + interest;
                debt -= principalPart;
                if (debt < 0)
                {
                    debt = 0;
                }

                schedule.Add(new PaymentScheduleItem
                {
                    MonthNumber = i,
                    Payment = payment,
                    Principal = principalPart,
                    Interest = interest,
                    RemainingDebt = debt
                });
            }

            return schedule;
        }
    }
}