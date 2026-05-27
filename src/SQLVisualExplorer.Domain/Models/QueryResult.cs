namespace SQLVisualExplorer.Domain.Models;

public sealed class QueryResult
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public TimeSpan Duration { get; init; }
    public long RowCount { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = [];
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];
}
