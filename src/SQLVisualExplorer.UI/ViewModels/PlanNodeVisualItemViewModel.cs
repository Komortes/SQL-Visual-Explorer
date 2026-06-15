using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class PlanNodeVisualItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private double _graphX;

    [ObservableProperty]
    private double _graphY;

    public Guid NodeId { get; init; }

    public string Label { get; init; } = string.Empty;

    public string NodeType { get; init; } = string.Empty;

    public string DepthLabel { get; init; } = string.Empty;

    public string CostText { get; init; } = string.Empty;

    public string RowsText { get; init; } = string.Empty;

    public string TimeText { get; init; } = string.Empty;

    public string IssueText { get; init; } = string.Empty;

    public string AccentColor { get; init; } = "#80B8FF";

    public string IssueColor { get; init; } = "#91A0AD";

    public string SelectionBorderBrush => IsSelected ? AccentColor : "#26313A";

    public string SelectionBackground => IsSelected ? "#17212A" : "#0F1419";

    public double CostBarWidth { get; init; }

    public double CostRatio { get; init; }

    public Thickness TreeIndentMargin { get; init; }

    public string IssueGlyph { get; init; } = "·";

    public string CostCell { get; init; } = "—";

    public string RowsCell { get; init; } = "—";

    public string TimeCell { get; init; } = "—";

    public double FlameBarWidth { get; init; }

    public string FlameMetricText { get; init; } = string.Empty;

    public Thickness IndentMargin { get; init; }

    public Thickness FlameMargin { get; init; }

    public string DetailText { get; init; } = string.Empty;

    public IReadOnlyList<PlanIssueItemViewModel> Issues { get; init; } = [];

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(SelectionBorderBrush));
        OnPropertyChanged(nameof(SelectionBackground));
    }

    public static PlanNodeVisualItemViewModel FromNode(
        PlanNode node,
        int depth,
        decimal? rootCost,
        double? rootActualTimeMs,
        IReadOnlyList<PlanIssueItemViewModel> issues)
    {
        var severity = PickMostSevereIssue(issues);
        var costRatio = CalculateCostRatio(node.TotalCost, rootCost);
        var flameRatio = CalculateFlameRatio(node, rootCost, rootActualTimeMs, costRatio);

        return new PlanNodeVisualItemViewModel
        {
            NodeId = node.Id,
            Label = node.Label,
            NodeType = node.NodeType.ToString(),
            DepthLabel = $"L{depth}",
            CostText = node.TotalCost is null ? "Cost n/a" : $"Cost {node.TotalCost.Value:N2}",
            RowsText = FormatRows(node),
            TimeText = node.ActualTotalTimeMs is null ? "Actual n/a" : $"Actual {node.ActualTotalTimeMs.Value:N2} ms",
            IssueText = severity?.ToString() ?? "OK",
            AccentColor = PickAccentColor(costRatio, severity),
            IssueColor = PickIssueColor(severity),
            CostBarWidth = Math.Clamp(costRatio * 160, 8, 160),
            CostRatio = costRatio,
            TreeIndentMargin = new Thickness(depth * 16, 0, 0, 0),
            IssueGlyph = severity switch
            {
                IssueSeverity.Critical => "✕",
                IssueSeverity.Warning => "⚠",
                IssueSeverity.Info => "i",
                _ => "·"
            },
            CostCell = node.TotalCost is null ? "—" : node.TotalCost.Value.ToString("N2"),
            RowsCell = node.ActualRows is not null
                ? node.ActualRows.Value.ToString("N0")
                : node.EstimatedRows is not null
                    ? node.EstimatedRows.Value.ToString("N0")
                    : "—",
            TimeCell = node.ActualTotalTimeMs is null ? "—" : $"{node.ActualTotalTimeMs.Value:N1} ms",
            FlameBarWidth = Math.Clamp(flameRatio * 620, 24, 620),
            FlameMetricText = FormatFlameMetric(node, flameRatio),
            IndentMargin = new Thickness(depth * 34, 0, 0, 10),
            FlameMargin = new Thickness(depth * 18, 0, 0, 8),
            DetailText = BuildDetailText(node, depth, costRatio),
            Issues = issues
        };
    }

    private static IssueSeverity? PickMostSevereIssue(IReadOnlyList<PlanIssueItemViewModel> issues)
    {
        if (issues.Any(issue => issue.Severity == IssueSeverity.Critical.ToString()))
        {
            return IssueSeverity.Critical;
        }

        if (issues.Any(issue => issue.Severity == IssueSeverity.Warning.ToString()))
        {
            return IssueSeverity.Warning;
        }

        return issues.Any(issue => issue.Severity == IssueSeverity.Info.ToString())
            ? IssueSeverity.Info
            : null;
    }

    private static string BuildDetailText(PlanNode node, int depth, double costRatio)
    {
        var estimatedRows = node.EstimatedRows is null ? "n/a" : node.EstimatedRows.Value.ToString("N0");
        var actualRows = node.ActualRows is null ? "n/a" : node.ActualRows.Value.ToString("N0");
        var cost = node.TotalCost is null ? "n/a" : node.TotalCost.Value.ToString("N2");
        var actualTime = node.ActualTotalTimeMs is null ? "n/a" : $"{node.ActualTotalTimeMs.Value:N2} ms";

        return $"Type: {node.NodeType}\nDepth: {depth}\nEstimated cost: {cost}\nRelative cost: {costRatio:P0}\nEstimated rows: {estimatedRows}\nActual rows: {actualRows}\nActual time: {actualTime}";
    }

    private static double CalculateFlameRatio(
        PlanNode node,
        decimal? rootCost,
        double? rootActualTimeMs,
        double costRatio)
    {
        if (node.ActualTotalTimeMs is not null && rootActualTimeMs is not null && rootActualTimeMs > 0)
        {
            return Math.Clamp(node.ActualTotalTimeMs.Value / rootActualTimeMs.Value, 0.04, 1.0);
        }

        return costRatio;
    }

    private static string FormatFlameMetric(PlanNode node, double flameRatio)
    {
        if (node.ActualTotalTimeMs is not null)
        {
            return $"{node.ActualTotalTimeMs.Value:N2} ms";
        }

        if (node.TotalCost is not null)
        {
            return $"{flameRatio:P0} cost";
        }

        return "n/a";
    }

    private static double CalculateCostRatio(decimal? nodeCost, decimal? rootCost)
    {
        if (nodeCost is null || rootCost is null || rootCost <= 0)
        {
            return 0.05;
        }

        return Math.Clamp((double)(nodeCost.Value / rootCost.Value), 0.05, 1.0);
    }

    private static string FormatRows(PlanNode node)
    {
        if (node.ActualRows is not null)
        {
            return $"Rows {node.ActualRows.Value:N0} actual";
        }

        if (node.EstimatedRows is not null)
        {
            return $"Rows {node.EstimatedRows.Value:N0} est.";
        }

        return "Rows n/a";
    }

    private static string PickAccentColor(double costRatio, IssueSeverity? severity)
    {
        if (severity == IssueSeverity.Critical)
        {
            return "#FF8A7A";
        }

        if (severity == IssueSeverity.Warning)
        {
            return "#FFD166";
        }

        if (costRatio >= 0.60)
        {
            return "#FF8A7A";
        }

        if (costRatio >= 0.20)
        {
            return "#FFD166";
        }

        return "#7BD88F";
    }

    private static string PickIssueColor(IssueSeverity? severity)
    {
        return severity switch
        {
            IssueSeverity.Critical => "#FF8A7A",
            IssueSeverity.Warning => "#FFD166",
            IssueSeverity.Info => "#80B8FF",
            _ => "#7BD88F"
        };
    }
}
