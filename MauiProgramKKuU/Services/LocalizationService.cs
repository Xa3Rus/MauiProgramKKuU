namespace MauiProgramKKuU.Services;

public static class LocalizationService
{
    private static readonly Dictionary<string, string> Ru = new()
    {
        ["Error"] = "Ошибка",
        ["Ok"] = "OK",
        ["InvalidData"] = "Проверьте введённые данные",
        ["InvalidAmount"] = "Введите корректную сумму",
        ["InvalidRate"] = "Введите корректную процентную ставку",
        ["InvalidMonths"] = "Введите корректный срок",
        ["FirstPayment"] = "Первый платеж",
        ["MonthlyPayment"] = "Ежемесячный платеж",
        ["TotalPayment"] = "Общая выплата",
        ["Overpayment"] = "Переплата",
        ["Warning"] = "Внимание",
        ["CalculateFirst"] = "Сначала выполните расчет",
        ["ExportDone"] = "Экспорт завершен",
        ["CsvSaved"] = "CSV сохранен",
        ["PdfSaved"] = "PDF сохранен"
    };

    private static readonly Dictionary<string, string> En = new()
    {
        ["Error"] = "Error",
        ["Ok"] = "OK",
        ["InvalidData"] = "Please check input values",
        ["InvalidAmount"] = "Enter a valid amount",
        ["InvalidRate"] = "Enter a valid interest rate",
        ["InvalidMonths"] = "Enter a valid term",
        ["FirstPayment"] = "First payment",
        ["MonthlyPayment"] = "Monthly payment",
        ["TotalPayment"] = "Total payment",
        ["Overpayment"] = "Overpayment",
        ["Warning"] = "Warning",
        ["CalculateFirst"] = "Please calculate first",
        ["ExportDone"] = "Export complete",
        ["CsvSaved"] = "CSV saved",
        ["PdfSaved"] = "PDF saved"
    };

    public static string T(string key)
    {
        var lang = AppSettingsService.Get().Language?.ToUpperInvariant() ?? "RU";
        var source = lang == "EN" ? En : Ru;
        return source.TryGetValue(key, out var value) ? value : key;
    }
}
