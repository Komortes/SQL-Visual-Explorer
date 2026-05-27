using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Services;

namespace SQLVisualExplorer.Infrastructure.Tests;

public sealed class PlanAnalyzerServiceTests
{
    [Fact]
    public void Analyze_FindsLargeSequentialScan()
    {
        var service = new PlanAnalyzerService();
        var plan = CreatePlan(new PlanNode
        {
            NodeType = NodeType.SeqScan,
            Label = "Seq Scan on orders",
            EstimatedRows = 25000
        });

        var issues = service.Analyze(plan);

        var issue = Assert.Single(issues);
        Assert.Equal("SEQ_SCAN_LARGE_TABLE", issue.Code);
        Assert.Equal(IssueSeverity.Critical, issue.Severity);
    }

    [Fact]
    public void Analyze_FindsRowEstimateMismatch()
    {
        var service = new PlanAnalyzerService();
        var plan = CreatePlan(new PlanNode
        {
            NodeType = NodeType.IndexScan,
            Label = "Index Scan on users",
            EstimatedRows = 10,
            ActualRows = 500
        });

        var issues = service.Analyze(plan);

        var issue = Assert.Single(issues);
        Assert.Equal("ROW_ESTIMATE_MISMATCH", issue.Code);
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Analyze_FindsHighCostChildNode()
    {
        var service = new PlanAnalyzerService();
        var plan = CreatePlan(new PlanNode
        {
            NodeType = NodeType.NestedLoop,
            Label = "Nested Loop",
            TotalCost = 100,
            Children =
            [
                new PlanNode
                {
                    NodeType = NodeType.IndexScan,
                    Label = "Index Scan on users",
                    TotalCost = 85
                }
            ]
        });

        var issues = service.Analyze(plan);

        var issue = Assert.Single(issues);
        Assert.Equal("HIGH_COST_NODE", issue.Code);
        Assert.Equal(IssueSeverity.Critical, issue.Severity);
    }

    private static ExecutionPlan CreatePlan(PlanNode root)
    {
        return new ExecutionPlan
        {
            Root = root,
            RawJson = "{}"
        };
    }
}
