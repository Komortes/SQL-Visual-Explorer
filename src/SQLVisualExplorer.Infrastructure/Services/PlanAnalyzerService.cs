using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class PlanAnalyzerService : IPlanAnalyzerService
{
    private const long LargeTableRowThreshold = 10_000;
    private const long SortRowThreshold = 1_000;
    private const long NestedLoopRowThreshold = 100_000;
    private const decimal HighCostRatio = 0.80m;
    private const double RowEstimateMismatchRatio = 10.0;

    public IReadOnlyList<PlanIssue> Analyze(ExecutionPlan executionPlan)
    {
        if (executionPlan.Root is null)
        {
            return [];
        }

        var issues = new List<PlanIssue>();
        var nodes = Flatten(executionPlan.Root).ToList();
        var totalCost = executionPlan.Root.TotalCost;

        foreach (var node in nodes)
        {
            AddSeqScanIssue(node, issues);
            AddNestedLoopIssue(node, issues);
            AddRowEstimateMismatchIssue(node, issues);
            AddSortIssue(node, issues);
            AddBitmapHeapScanInfo(node, issues);

            if (node.Id != executionPlan.Root.Id)
            {
                AddHighCostIssue(node, totalCost, issues);
            }
        }

        return issues;
    }

    private static IEnumerable<PlanNode> Flatten(PlanNode node)
    {
        yield return node;

        foreach (var child in node.Children)
        {
            foreach (var descendant in Flatten(child))
            {
                yield return descendant;
            }
        }
    }

    private static void AddSeqScanIssue(PlanNode node, List<PlanIssue> issues)
    {
        var rowCount = GetBestRowCount(node);

        if (node.NodeType != NodeType.SeqScan || rowCount < LargeTableRowThreshold)
        {
            return;
        }

        issues.Add(new PlanIssue
        {
            Code = "SEQ_SCAN_LARGE_TABLE",
            Severity = IssueSeverity.Critical,
            Title = "Sequential scan on a large row set",
            Description = $"{node.Label} scans about {rowCount:N0} row(s).",
            Recommendation = "Check WHERE and JOIN predicates and add an index for the filtered or joined columns.",
            PlanNodeId = node.Id
        });
    }

    private static void AddNestedLoopIssue(PlanNode node, List<PlanIssue> issues)
    {
        var rowCount = GetBestRowCount(node);

        if (node.NodeType != NodeType.NestedLoop || rowCount < NestedLoopRowThreshold)
        {
            return;
        }

        issues.Add(new PlanIssue
        {
            Code = "NESTED_LOOP_LARGE",
            Severity = IssueSeverity.Critical,
            Title = "Large nested loop",
            Description = $"{node.Label} may iterate over about {rowCount:N0} row(s).",
            Recommendation = "Check join predicates and indexes. A hash join or better index may reduce repeated lookups.",
            PlanNodeId = node.Id
        });
    }

    private static void AddHighCostIssue(PlanNode node, decimal? totalCost, List<PlanIssue> issues)
    {
        if (node.TotalCost is null || totalCost is null || totalCost <= 0)
        {
            return;
        }

        var ratio = node.TotalCost.Value / totalCost.Value;

        if (ratio < HighCostRatio)
        {
            return;
        }

        issues.Add(new PlanIssue
        {
            Code = "HIGH_COST_NODE",
            Severity = IssueSeverity.Critical,
            Title = "High cost plan node",
            Description = $"{node.Label} accounts for about {ratio:P0} of the estimated root cost.",
            Recommendation = "Start optimization from this node and inspect its filters, joins, indexes, and row estimates.",
            PlanNodeId = node.Id
        });
    }

    private static void AddRowEstimateMismatchIssue(PlanNode node, List<PlanIssue> issues)
    {
        if (node.EstimatedRows is null or <= 0 || node.ActualRows is null or <= 0)
        {
            return;
        }

        var high = Math.Max(node.EstimatedRows.Value, node.ActualRows.Value);
        var low = Math.Min(node.EstimatedRows.Value, node.ActualRows.Value);
        var ratio = (double)high / low;

        if (ratio < RowEstimateMismatchRatio)
        {
            return;
        }

        issues.Add(new PlanIssue
        {
            Code = "ROW_ESTIMATE_MISMATCH",
            Severity = IssueSeverity.Warning,
            Title = "Planner row estimate mismatch",
            Description = $"{node.Label} estimated {node.EstimatedRows:N0} row(s), actual was {node.ActualRows:N0}.",
            Recommendation = "Refresh database statistics with ANALYZE and check whether predicates need better statistics.",
            PlanNodeId = node.Id
        });
    }

    private static void AddSortIssue(PlanNode node, List<PlanIssue> issues)
    {
        var rowCount = GetBestRowCount(node);

        if (node.NodeType != NodeType.Sort || rowCount < SortRowThreshold)
        {
            return;
        }

        issues.Add(new PlanIssue
        {
            Code = "SORT_WITHOUT_INDEX",
            Severity = IssueSeverity.Warning,
            Title = "Large sort operation",
            Description = $"{node.Label} sorts about {rowCount:N0} row(s).",
            Recommendation = "If this comes from ORDER BY, consider an index that matches the sort columns.",
            PlanNodeId = node.Id
        });
    }

    private static void AddBitmapHeapScanInfo(PlanNode node, List<PlanIssue> issues)
    {
        if (node.NodeType != NodeType.BitmapHeapScan)
        {
            return;
        }

        issues.Add(new PlanIssue
        {
            Code = "BITMAP_HEAP_SCAN",
            Severity = IssueSeverity.Info,
            Title = "Bitmap heap scan",
            Description = $"{node.Label} uses a bitmap heap scan.",
            Recommendation = "This can be normal. For highly selective queries, check whether an index-only scan is possible.",
            PlanNodeId = node.Id
        });
    }

    private static long GetBestRowCount(PlanNode node)
    {
        return node.ActualRows ?? node.EstimatedRows ?? 0;
    }
}
