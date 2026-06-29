using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanRunQuery))]
    private async Task RunQueryAsync()
    {
        if (SelectedConnection is null)
        {
            QueryStatusMessage = "Select a connection first.";
            return;
        }

        QueryStatusMessage = "Running query...";
        ResultColumns.Clear();
        ResultColumnsView.Clear();
        ResultRows.Clear();
        VisualPlanNodes.Clear();
        PlanTreeRoots.Clear();
        PlanIssues.Clear();
        SelectedPlanNodeIssues.Clear();
        ResultHeaderText = string.Empty;
        PlanTreeHeaderText = "Plan Tree";
        SelectedPlanNodeTitle = "No plan node selected.";
        SelectedPlanNodeDetails = "Run Explain and select a node to inspect details.";

        var stopwatch = Stopwatch.StartNew();
        IsBusy = true;
        _cts = new CancellationTokenSource();

        try
        {
            var result = await _queryExecutionService.ExecuteAsync(
                CreateExecutableConnection(), SqlText, _cts.Token);
            stopwatch.Stop();

            BuildResultGrid(result.Columns, result.Rows);
            ExportResultsToCsvCommand.NotifyCanExecuteChanged();

            QueryStatusMessage =
                $"Returned {result.RowCount} row(s) in {result.Duration.TotalMilliseconds:N0} ms.";
            await RecordHistoryAsync(true, result.Duration, result.RowCount, null);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            QueryStatusMessage = "Query cancelled.";
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            QueryStatusMessage = GetFriendlyErrorMessage(exception);
            await RecordHistoryAsync(false, stopwatch.Elapsed, null, exception.Message);
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunQuery))]
    private async Task ExplainQueryAsync()
    {
        await ExplainQueryCoreAsync(
            "Running explain...",
            "Explain is running...",
            (connection, sql, token) => _explainAnalyzeService.ExplainAsync(connection, sql, token),
            "Explain");
    }

    [RelayCommand(CanExecute = nameof(CanRunQuery))]
    private async Task ExplainAnalyzeQueryAsync()
    {
        await ExplainQueryCoreAsync(
            "Running explain analyze...",
            "Explain Analyze is running. The database will execute the query.",
            (connection, sql, token) => _explainAnalyzeService.ExplainAnalyzeAsync(connection, sql, token),
            "Explain Analyze");
    }

    private async Task ExplainQueryCoreAsync(
        string runningStatus,
        string runningSummary,
        Func<Connection, string, CancellationToken, Task<ExecutionPlan>> explainFunc,
        string label)
    {
        if (SelectedConnection is null)
        {
            QueryStatusMessage = "Select a connection first.";
            return;
        }

        QueryStatusMessage = runningStatus;
        ResultColumns.Clear();
        ResultColumnsView.Clear();
        ResultRows.Clear();
        VisualPlanNodes.Clear();
        GraphEdges.Clear();
        PlanTreeRoots.Clear();
        PlanIssues.Clear();
        SelectedPlanNodeIssues.Clear();
        ResultHeaderText = string.Empty;
        PlanSummaryText = runningSummary;
        HasPlanIssues = false;
        PlanIssuesBadgeText = string.Empty;
        SelectedPlanNodeTitle = "No plan node selected.";
        SelectedPlanNodeDetails = "Explain is running.";

        var stopwatch = Stopwatch.StartNew();
        IsBusy = true;
        _cts = new CancellationTokenSource();

        try
        {
            var plan = await explainFunc(CreateExecutableConnection(), SqlText, _cts.Token);
            stopwatch.Stop();

            var planNodeCount = PresentPlan(plan);
            ExportResultsToCsvCommand.NotifyCanExecuteChanged();
            QueryStatusMessage =
                $"{label} returned {planNodeCount} plan node(s), {plan.Issues.Count} issue(s) " +
                $"in {stopwatch.Elapsed.TotalMilliseconds:N0} ms.";
            SelectedNavigationItem = NavigationItems.First(item => item.Code == "PL");
            await RecordHistoryAsync(true, stopwatch.Elapsed, planNodeCount, null, plan.RawJson);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            QueryStatusMessage = $"{label} cancelled.";
            PlanSummaryText = "Cancelled.";
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            var friendlyMessage = GetFriendlyErrorMessage(exception);
            QueryStatusMessage = friendlyMessage;
            PlanSummaryText = $"{label} failed.";
            PlanTreeHeaderText = "Plan Tree";
            SelectedPlanNodeTitle = "No plan node selected.";
            SelectedPlanNodeDetails = friendlyMessage;
            await RecordHistoryAsync(false, stopwatch.Elapsed, null, exception.Message);
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private bool CanRunQuery() =>
        SelectedConnection is not null && !string.IsNullOrWhiteSpace(SqlText) && !IsBusy;

    [RelayCommand]
    private void CancelRun() => _cts?.Cancel();

    [RelayCommand]
    private void ShowAbout() =>
        QueryStatusMessage = "SQL Visual Explorer — analyze EXPLAIN plans fast. Tree, graph and grid views.";

    [RelayCommand]
    private void FormatSql()
    {
        if (!string.IsNullOrWhiteSpace(SqlText))
            SqlText = SqlFormatter.Format(SqlText);
    }

    private async Task RecordHistoryAsync(
        bool succeeded,
        TimeSpan? duration,
        long? rowCount,
        string? errorMessage,
        string? explainJson = null)
    {
        var entry = await _historyService.RecordAsync(new RecordQueryHistoryRequest
        {
            ConnectionId = SelectedConnection?.Id,
            DatabaseType = SelectedConnection?.Connection.DatabaseType,
            SqlText = SqlText,
            Duration = duration,
            RowCount = rowCount,
            Succeeded = succeeded,
            ErrorMessage = errorMessage,
            ExplainJson = explainJson
        });

        HistoryItems.Insert(0, QueryHistoryItemViewModel.FromEntry(entry));
    }
}
