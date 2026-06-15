namespace SQLVisualExplorer.Domain.Models;

public sealed record AdvisorResult
{
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Suggestions { get; init; } = [];
    public string RewrittenSql { get; init; } = string.Empty;
}
