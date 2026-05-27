namespace SQLVisualExplorer.Domain.Models;

public sealed class QueryHistoryEntry
{
    public Guid Id { get; init; }

    public Guid? ConnectionId { get; init; }

    public string ConnectionName { get; init; } = string.Empty;

    public string SqlText { get; init; } = string.Empty;

    public DateTimeOffset ExecutedAt { get; init; }

    public TimeSpan? Duration { get; init; }

    public long? RowCount { get; init; }

    public string Status { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }

    public string? ExplainJson { get; init; }
}
