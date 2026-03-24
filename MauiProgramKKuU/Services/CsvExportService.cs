using MauiProgramKKuU.Models;
using System.Text;

namespace MauiProgramKKuU.Services;

public static class CsvExportService
{
    public static async Task<string> ExportScheduleAsync(IEnumerable<PaymentScheduleItem> schedule, string title)
    {
        var safeTitle = string.Concat(title.Where(char.IsLetterOrDigit));
        if (string.IsNullOrWhiteSpace(safeTitle))
        {
            safeTitle = "schedule";
        }

        var fileName = $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("Month,Payment,Principal,Interest,RemainingDebt");

        foreach (var item in schedule)
        {
            sb.AppendLine($"{item.MonthNumber},{item.Payment:F2},{item.Principal:F2},{item.Interest:F2},{item.RemainingDebt:F2}");
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
        return filePath;
    }
}
