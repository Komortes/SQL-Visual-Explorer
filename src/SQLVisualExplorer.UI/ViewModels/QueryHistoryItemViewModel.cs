using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed class QueryHistoryItemViewModel
{
    public Guid Id { get; init; }

    public string ConnectionName { get; init; } = string.Empty;

    public string SqlPreview { get; init; } = string.Empty;

    public string SqlText { get; init; } = string.Empty;

    public string ExecutedAt { get; init; } = string.Empty;

    public string Duration { get; init; } = string.Empty;

    public string RowCount { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public static QueryHistoryItemViewModel FromEntry(QueryHistoryEntry entry)
    {
        return new QueryHistoryItemViewModel
        {
            Id = entry.Id,
            ConnectionName = entry.ConnectionName,
            SqlText = entry.SqlText,
            SqlPreview = CreatePreview(entry.SqlText),
            ExecutedAt = entry.ExecutedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            Duration = entry.Duration is null ? "-" : $"{entry.Duration.Value.TotalMilliseconds:N0} ms",
            RowCount = entry.RowCount is null ? "-" : entry.RowCount.Value.ToString("N0"),
            Status = entry.Status,
            ErrorMessage = entry.ErrorMessage ?? string.Empty
        };
    }

    private static string CreatePreview(string sql)
    {
        var collapsed = string.Join(" ", sql.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return collapsed.Length <= 90
            ? collapsed
            : string.Concat(collapsed.AsSpan(0, 90), "...");
    }
}
