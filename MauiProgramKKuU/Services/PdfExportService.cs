using MauiProgramKKuU.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MauiProgramKKuU.Services;

public static class PdfExportService
{
    public static async Task<string> ExportScheduleAsync(IEnumerable<PaymentScheduleItem> schedule, string title)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var safeTitle = string.Concat(title.Where(char.IsLetterOrDigit));
        if (string.IsNullOrWhiteSpace(safeTitle))
        {
            safeTitle = "schedule";
        }

        var fileName = $"{safeTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
        var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
        var rows = schedule.ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Size(PageSizes.A4);
                page.Header().Text(title).FontSize(18).Bold();
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(45);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("M");
                        header.Cell().Text("Payment");
                        header.Cell().Text("Principal");
                        header.Cell().Text("Interest");
                        header.Cell().Text("Debt");
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Text(r.MonthNumber.ToString());
                        table.Cell().Text(r.Payment.ToString("F2"));
                        table.Cell().Text(r.Principal.ToString("F2"));
                        table.Cell().Text(r.Interest.ToString("F2"));
                        table.Cell().Text(r.RemainingDebt.ToString("F2"));
                    }
                });
            });
        });

        await Task.Run(() => document.GeneratePdf(filePath));
        return filePath;
    }
}
