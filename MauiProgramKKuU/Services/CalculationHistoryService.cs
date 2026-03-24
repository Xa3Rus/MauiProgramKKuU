using MauiProgramKKuU.Models;
using System.Text.Json;

namespace MauiProgramKKuU.Services;

public static class CalculationHistoryService
{
    private const string HistoryKey = "loan_history_v1";
    private const int MaxItems = 50;

    public static List<LoanHistoryItem> GetAll()
    {
        try
        {
            var json = Preferences.Get(HistoryKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return [];
            }

            return JsonSerializer.Deserialize<List<LoanHistoryItem>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static void Add(LoanHistoryItem item)
    {
        var all = GetAll();
        all.Insert(0, item);
        if (all.Count > MaxItems)
        {
            all = all.Take(MaxItems).ToList();
        }

        Preferences.Set(HistoryKey, JsonSerializer.Serialize(all));
    }

    public static void Clear()
    {
        Preferences.Remove(HistoryKey);
    }
}
