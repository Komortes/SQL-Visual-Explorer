using SQLVisualExplorer.Domain.Enums;

namespace SQLVisualExplorer.Domain.Models;

public sealed class PlanIssue
{
    public string Code { get; init; } = string.Empty;
    public IssueSeverity Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public Guid? PlanNodeId { get; init; }
}
