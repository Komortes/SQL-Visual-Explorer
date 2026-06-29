using CommunityToolkit.Mvvm.Input;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private void SelectPlanNode(PlanNodeVisualItemViewModel? node)
    {
        foreach (var visualNode in VisualPlanNodes)
            visualNode.IsSelected = ReferenceEquals(visualNode, node);

        SelectedPlanNodeIssues.Clear();

        if (node is null)
        {
            SelectedPlanNodeTitle = "No plan node selected.";
            SelectedPlanNodeDetails = "Select a node in the plan tree to inspect details.";
            return;
        }

        SelectedPlanNodeTitle = node.Label;
        SelectedPlanNodeDetails = node.DetailText;

        foreach (var issue in node.Issues)
            SelectedPlanNodeIssues.Add(issue);
    }

    private int PresentPlan(ExecutionPlan plan)
    {
        VisualPlanNodes.Clear();
        GraphEdges.Clear();
        PlanTreeRoots.Clear();
        PlanIssues.Clear();
        SelectedPlanNodeIssues.Clear();

        var flattenedNodes = FlattenPlan(plan.Root).ToList();
        var issueItemsByNodeId = BuildIssueIndex(plan.Issues);

        foreach (var item in flattenedNodes)
        {
            VisualPlanNodes.Add(PlanNodeVisualItemViewModel.FromNode(
                item.Node,
                item.Depth,
                plan.Root?.TotalCost,
                plan.Root?.ActualTotalTimeMs,
                issueItemsByNodeId.GetValueOrDefault(item.Node.Id) ?? []));
        }

        foreach (var issue in plan.Issues)
            PlanIssues.Add(PlanIssueItemViewModel.FromIssue(issue));

        _currentPlanIssues = plan.Issues;
        PlanSummaryText = BuildPlanSummary(plan.Root, flattenedNodes.Count, plan.Issues.Count);
        UpdateIssuesBadge(plan.Issues);
        PlanTreeHeaderText = $"Plan ({flattenedNodes.Count} node(s))";

        if (plan.Root is not null)
        {
            ApplyGraphLayout(plan.Root);
            BuildPlanTree(plan.Root, issueItemsByNodeId);
        }

        SelectPlanNode(VisualPlanNodes.FirstOrDefault());
        ExportPlanToHtmlCommand.NotifyCanExecuteChanged();
        return flattenedNodes.Count;
    }

    [RelayCommand]
    private void ShowPlanTable()
    {
        IsPlanTableVisible = true;
        IsPlanGraphVisible = false;
        IsPlanFlamegraphVisible = false;
    }

    [RelayCommand]
    private void ShowPlanGraph()
    {
        IsPlanTableVisible = false;
        IsPlanGraphVisible = true;
        IsPlanFlamegraphVisible = false;
    }

    [RelayCommand]
    private void ShowPlanFlamegraph()
    {
        IsPlanTableVisible = false;
        IsPlanGraphVisible = false;
        IsPlanFlamegraphVisible = true;
    }

    private void BuildPlanTree(
        PlanNode root,
        IReadOnlyDictionary<Guid, IReadOnlyList<PlanIssueItemViewModel>> issueIndex)
    {
        PlanTreeRoots.Clear();
        PlanTreeRoots.Add(PlanNodeRowViewModel.FromNode(
            root, root.TotalCost, root.ActualTotalTimeMs, issueIndex));
    }

    [RelayCommand]
    private void SelectPlanTreeNode(PlanNodeRowViewModel? node)
    {
        if (node is null) return;
        var visualNode = VisualPlanNodes.FirstOrDefault(n => n.NodeId == node.NodeId);
        SelectPlanNode(visualNode);
        ApplyTreeSelection(PlanTreeRoots, node.NodeId);
    }

    private static void ApplyTreeSelection(
        IEnumerable<PlanNodeRowViewModel> nodes, Guid selectedId)
    {
        foreach (var n in nodes)
        {
            n.IsSelected = n.NodeId == selectedId;
            ApplyTreeSelection(n.Children, selectedId);
        }
    }

    private void ApplyGraphLayout(PlanNode root)
    {
        var positions = GraphLayoutEngine.Arrange(root);

        foreach (var vm in VisualPlanNodes)
        {
            if (positions.TryGetValue(vm.NodeId, out var pos))
            {
                vm.GraphX = pos.X;
                vm.GraphY = pos.Y;
            }
        }

        GraphEdges.Clear();
        BuildEdges(root, positions);

        var maxX = positions.Values.Select(p => p.X).DefaultIfEmpty(0).Max();
        var maxY = positions.Values.Select(p => p.Y).DefaultIfEmpty(0).Max();
        GraphWidth  = maxX + GraphLayoutEngine.NodeWidth  + 40;
        GraphHeight = maxY + GraphLayoutEngine.NodeHeight + 40;
        GraphZoom   = ComputeFitZoom?.Invoke() ?? 1.0;
    }

    private void BuildEdges(
        PlanNode node,
        IReadOnlyDictionary<Guid, (double X, double Y)> positions)
    {
        if (!positions.TryGetValue(node.Id, out var parentPos)) return;

        foreach (var child in node.Children)
        {
            if (positions.TryGetValue(child.Id, out var childPos))
            {
                GraphEdges.Add(new GraphEdgeViewModel
                {
                    X1 = parentPos.X + GraphLayoutEngine.NodeWidth / 2,
                    Y1 = parentPos.Y + GraphLayoutEngine.NodeHeight,
                    X2 = childPos.X  + GraphLayoutEngine.NodeWidth / 2,
                    Y2 = childPos.Y,
                });
            }

            BuildEdges(child, positions);
        }
    }

    private void UpdateIssuesBadge(IReadOnlyList<PlanIssue> issues)
    {
        HasPlanIssues = issues.Count > 0;
        PlanIssuesBadgeText = $"{issues.Count} issue(s)";
        PlanIssuesBadgeColor = issues.Any(i => i.Severity == IssueSeverity.Critical)
            ? "#FF8A7A"
            : issues.Any(i => i.Severity == IssueSeverity.Warning)
                ? "#FFD166"
                : "#80B8FF";
    }

    private static string BuildPlanSummary(PlanNode? root, int nodeCount, int issueCount)
    {
        if (root is null)
            return "Explain returned no plan nodes.";

        var cost = root.TotalCost is null ? "n/a" : root.TotalCost.Value.ToString("N2");
        var rows = root.EstimatedRows is null ? "n/a" : root.EstimatedRows.Value.ToString("N0");
        var actualTime = root.ActualTotalTimeMs is null
            ? "n/a"
            : $"{root.ActualTotalTimeMs.Value:N2} ms";

        return $"Root: {root.Label}\nNodes: {nodeCount}\nIssues: {issueCount}" +
               $"\nEstimated cost: {cost}\nEstimated rows: {rows}\nActual time: {actualTime}";
    }
}
