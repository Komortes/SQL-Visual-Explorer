using CommunityToolkit.Mvvm.ComponentModel;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class QueryHistoryItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isPendingDelete;

    public Guid Id { get; init; }

    public string ConnectionName { get; init; } = string.Empty;

    public DatabaseType? DatabaseType { get; init; }

    public string SqlPreview { get; init; } = string.Empty;

    public string SqlText { get; init; } = string.Empty;

    public string ExecutedAt { get; init; } = string.Empty;

    public string Duration { get; init; } = string.Empty;

    public string RowCount { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public string StatusBadgeText { get; init; } = string.Empty;

    public string StatusBadgeBackground { get; init; } = "#17212A";

    public string StatusBadgeForeground { get; init; } = "#91A0AD";

    public double? DurationMs { get; init; }

    public string? ExplainOutput { get; init; }

    public bool HasSavedPlan => !string.IsNullOrWhiteSpace(ExplainOutput) && DatabaseType is not null;

    public static QueryHistoryItemViewModel FromEntry(QueryHistoryEntry entry)
    {
        var (badgeText, badgeBg, badgeFg) = entry.Status switch
        {
            "success" => ("OK",   "#0F2014", "#7BD88F"),
            "error"   => ("ERR",  "#1F0F0F", "#FF8A7A"),
            _         => ("PLAN", "#0D1829", "#80B8FF"),
        };

        return new QueryHistoryItemViewModel
        {
            Id = entry.Id,
            ConnectionName = entry.ConnectionName,
            DatabaseType = entry.DatabaseType,
            SqlText = entry.SqlText,
            SqlPreview = CreatePreview(entry.SqlText),
            ExecutedAt = entry.ExecutedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
            Duration = entry.Duration is null ? "-" : $"{entry.Duration.Value.TotalMilliseconds:N0} ms",
            RowCount = entry.RowCount is null ? "-" : entry.RowCount.Value.ToString("N0"),
            Status = entry.Status,
            ErrorMessage = entry.ErrorMessage ?? string.Empty,
            StatusBadgeText = badgeText,
            StatusBadgeBackground = badgeBg,
            StatusBadgeForeground = badgeFg,
            DurationMs = entry.Duration?.TotalMilliseconds,
            ExplainOutput = entry.ExplainJson,
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
