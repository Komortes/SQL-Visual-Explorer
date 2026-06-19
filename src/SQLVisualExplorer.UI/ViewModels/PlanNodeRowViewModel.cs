using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class PlanNodeRowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isSelected;

    public Guid NodeId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string CostCell { get; init; } = "—";
    public string RowsCell { get; init; } = "—";
    public string TimeCell { get; init; } = "—";
    public string AccentColor { get; init; } = "#80B8FF";
    public string IssueGlyph { get; init; } = "·";
    public string IssueColor { get; init; } = "#91A0AD";
    public IReadOnlyList<PlanIssueItemViewModel> Issues { get; init; } = [];
    public ObservableCollection<PlanNodeRowViewModel> Children { get; } = [];

    public string SelectionBorderBrush => IsSelected ? AccentColor : "#26313A";
    public string SelectionBackground => IsSelected ? "#17212A" : "#0F1419";

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(SelectionBorderBrush));
        OnPropertyChanged(nameof(SelectionBackground));
    }

    public static PlanNodeRowViewModel FromNode(
        PlanNode node,
        decimal? rootCost,
        double? rootActualTimeMs,
        IReadOnlyDictionary<Guid, IReadOnlyList<PlanIssueItemViewModel>> issuesByNodeId)
    {
        var issues = issuesByNodeId.GetValueOrDefault(node.Id) ?? [];
        var severity = PickMostSevereIssue(issues);
        var costRatio = CalculateCostRatio(node.TotalCost, rootCost);

        var vm = new PlanNodeRowViewModel
        {
            NodeId = node.Id,
            Label = node.Label,
            CostCell = node.TotalCost is null ? "—" : node.TotalCost.Value.ToString("N2"),
            RowsCell = node.ActualRows is not null
                ? node.ActualRows.Value.ToString("N0")
                : node.EstimatedRows is not null
                    ? node.EstimatedRows.Value.ToString("N0")
                    : "—",
            TimeCell = node.ActualTotalTimeMs is null ? "—" : $"{node.ActualTotalTimeMs.Value:N1} ms",
            AccentColor = PickAccentColor(costRatio, severity),
            IssueGlyph = severity switch
            {
                IssueSeverity.Critical => "✕",
                IssueSeverity.Warning  => "⚠",
                IssueSeverity.Info     => "i",
                _                      => "·"
            },
            IssueColor = severity switch
            {
                IssueSeverity.Critical => "#FF8A7A",
                IssueSeverity.Warning  => "#FFD166",
                IssueSeverity.Info     => "#80B8FF",
                _                      => "#7BD88F"
            },
            Issues = issues,
        };

        foreach (var child in node.Children)
            vm.Children.Add(FromNode(child, rootCost, rootActualTimeMs, issuesByNodeId));

        return vm;
    }

    private static IssueSeverity? PickMostSevereIssue(IReadOnlyList<PlanIssueItemViewModel> issues)
    {
        if (issues.Any(i => i.Severity == IssueSeverity.Critical.ToString())) return IssueSeverity.Critical;
        if (issues.Any(i => i.Severity == IssueSeverity.Warning.ToString()))  return IssueSeverity.Warning;
        return issues.Any(i => i.Severity == IssueSeverity.Info.ToString()) ? IssueSeverity.Info : null;
    }

    private static double CalculateCostRatio(decimal? nodeCost, decimal? rootCost)
    {
        if (nodeCost is null || rootCost is null || rootCost <= 0) return 0.05;
        return Math.Clamp((double)(nodeCost.Value / rootCost.Value), 0.05, 1.0);
    }

    private static string PickAccentColor(double costRatio, IssueSeverity? severity)
    {
        if (severity == IssueSeverity.Critical) return "#FF8A7A";
        if (severity == IssueSeverity.Warning)  return "#FFD166";
        if (costRatio >= 0.60) return "#FF8A7A";
        if (costRatio >= 0.20) return "#FFD166";
        return "#7BD88F";
    }
}
