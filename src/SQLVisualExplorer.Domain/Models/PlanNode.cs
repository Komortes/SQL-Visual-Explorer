using SQLVisualExplorer.Domain.Enums;

namespace SQLVisualExplorer.Domain.Models;

public sealed class PlanNode
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public NodeType NodeType { get; init; } = NodeType.Unknown;
    public string Label { get; init; } = string.Empty;
    public decimal? TotalCost { get; init; }
    public double? ActualTotalTimeMs { get; init; }
    public long? EstimatedRows { get; init; }
    public long? ActualRows { get; init; }
    public long? ActualLoops { get; init; }
    public string? RelationName { get; init; }
    public string? IndexName { get; init; }
    public IReadOnlyList<PlanNode> Children { get; init; } = [];

    public string? Filter { get; init; }

    public string? JoinCondition { get; init; }

    public int? HashBatches { get; init; }
}
