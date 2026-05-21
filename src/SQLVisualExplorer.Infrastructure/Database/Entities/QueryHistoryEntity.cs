namespace SQLVisualExplorer.Infrastructure.Database.Entities;

public sealed class QueryHistoryEntity
{
    public string Id { get; set; } = string.Empty;

    public string? ConnectionId { get; set; }

    public ConnectionEntity? Connection { get; set; }

    public string SqlText { get; set; } = string.Empty;

    public DateTime ExecutedAt { get; set; }

    public long? DurationMs { get; set; }

    public long? RowCount { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? ExplainJson { get; set; }
}
