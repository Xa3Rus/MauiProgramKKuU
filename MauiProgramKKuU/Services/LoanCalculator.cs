using System;

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
    }
}