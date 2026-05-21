namespace SQLVisualExplorer.Domain.Models;

public sealed class ExecutionPlan
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public PlanNode? Root { get; init; }
    public string? RawJson { get; init; }
    public IReadOnlyList<PlanIssue> Issues { get; init; } = [];
}
