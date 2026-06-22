using SQLVisualExplorer.Domain.Enums;

namespace SQLVisualExplorer.Application.Services;

public sealed class RecordQueryHistoryRequest
{
    public Guid? ConnectionId { get; init; }

    public DatabaseType? DatabaseType { get; init; }

    public string SqlText { get; init; } = string.Empty;

    public TimeSpan? Duration { get; init; }

    public long? RowCount { get; init; }

    public bool Succeeded { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ExplainJson { get; init; }
}
