using CommunityToolkit.Mvvm.Input;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanRunCompare))]
    private async Task RunCompareAsync()
    {
        IsCompareRunning = true;
        IsBusy = true;
        _cts = new CancellationTokenSource();
        CompareStatusMessage = "Running EXPLAIN ANALYZE on both queries…";
        CompareHasResults = false;
        CompareResultA = null;
        CompareResultB = null;

        try
        {
            var conn  = CreateExecutableConnection();
            var taskA = _explainAnalyzeService.ExplainAnalyzeAsync(conn, CompareQueryAText, _cts.Token);
            var taskB = _explainAnalyzeService.ExplainAnalyzeAsync(conn, CompareQueryBText, _cts.Token);
            await Task.WhenAll(taskA, taskB);

            var resultA = BuildCompareResult("Query A", taskA.Result);
            var resultB = BuildCompareResult("Query B", taskB.Result);
            DetermineCompareWinner(resultA, resultB, taskA.Result, taskB.Result);
            PopulateComparePlanNodes(ComparePlanNodesA, taskA.Result);
            PopulateComparePlanNodes(ComparePlanNodesB, taskB.Result);

            CompareResultA    = resultA;
            CompareResultB    = resultB;
            CompareHasResults = true;

            var winner = resultA.IsWinner ? "A" : resultB.IsWinner ? "B" : "Tie";
            CompareStatusMessage = winner == "Tie"
                ? "Done — no clear winner (equal cost/time)."
                : $"Done — Query {winner} is faster.";
        }
        catch (OperationCanceledException)
        {
            CompareStatusMessage = "Compare cancelled.";
        }
        catch (Exception ex)
        {
            CompareStatusMessage = GetFriendlyErrorMessage(ex);
        }
        finally
        {
            IsCompareRunning = false;
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private bool CanRunCompare() =>
        SelectedConnection is not null &&
        !string.IsNullOrWhiteSpace(CompareQueryAText) &&
        !string.IsNullOrWhiteSpace(CompareQueryBText) &&
        !IsCompareRunning &&
        !IsBusy;

    private static ComparePlanResultViewModel BuildCompareResult(string label, ExecutionPlan plan)
    {
        var root      = plan.Root;
        var cost      = root?.TotalCost is null         ? "n/a" : root.TotalCost.Value.ToString("N2");
        var time      = root?.ActualTotalTimeMs is null ? "n/a" : $"{root.ActualTotalTimeMs.Value:N2} ms";
        var estRows   = root?.EstimatedRows is null     ? "n/a" : root.EstimatedRows.Value.ToString("N0");
        var actRows   = root?.ActualRows is null        ? "n/a" : root.ActualRows.Value.ToString("N0");
        var nodeCount = FlattenPlan(root).Count();
        var issues    = plan.Issues;

        var issueColor = issues.Any(i => i.Severity == IssueSeverity.Critical) ? "#FF8A7A" :
                         issues.Any(i => i.Severity == IssueSeverity.Warning)  ? "#FFD166" :
                         issues.Count > 0                                       ? "#80B8FF" : "#7BD88F";

        return new ComparePlanResultViewModel
        {
            Label         = label,
            RootLabel     = root?.Label ?? "No plan returned",
            CostText      = cost,
            TimeText      = time,
            EstRowsText   = estRows,
            ActRowsText   = actRows,
            NodeCountText = $"{nodeCount} node(s)",
            IssueText     = issues.Count == 0 ? "No issues" : $"{issues.Count} issue(s)",
            IssueColor    = issueColor,
        };
    }

    private static void DetermineCompareWinner(
        ComparePlanResultViewModel a,
        ComparePlanResultViewModel b,
        ExecutionPlan planA,
        ExecutionPlan planB)
    {
        var timeA = planA.Root?.ActualTotalTimeMs;
        var timeB = planB.Root?.ActualTotalTimeMs;
        if (timeA.HasValue && timeB.HasValue)
        {
            if (timeA.Value < timeB.Value) a.IsWinner = true;
            else if (timeB.Value < timeA.Value) b.IsWinner = true;
            return;
        }

        var costA = planA.Root?.TotalCost;
        var costB = planB.Root?.TotalCost;
        if (costA.HasValue && costB.HasValue)
        {
            if (costA.Value < costB.Value) a.IsWinner = true;
            else if (costB.Value < costA.Value) b.IsWinner = true;
        }
    }

    private static void PopulateComparePlanNodes(
        System.Collections.ObjectModel.ObservableCollection<PlanNodeVisualItemViewModel> target,
        ExecutionPlan plan)
    {
        target.Clear();
        if (plan.Root is null) return;
        var rootCost = plan.Root.TotalCost;
        var rootTime = plan.Root.ActualTotalTimeMs;
        foreach (var (node, depth) in FlattenPlan(plan.Root))
        {
            var nodeIssues = plan.Issues
                .Where(i => i.PlanNodeId == node.Id)
                .Select(PlanIssueItemViewModel.FromIssue)
                .ToList();
            target.Add(PlanNodeVisualItemViewModel.FromNode(node, depth, rootCost, rootTime, nodeIssues));
        }
    }
}
