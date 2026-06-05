using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IConnectionService _connectionService;
    private readonly IQueryExecutionService _queryExecutionService;
    private readonly IExplainAnalyzeService _explainAnalyzeService;
    private readonly IHistoryService _historyService;

    [ObservableProperty]
    private ShellNavigationItemViewModel _selectedNavigationItem;

    [ObservableProperty]
    private bool _isConnectionsVisible;

    [ObservableProperty]
    private bool _isHistoryVisible;

    [ObservableProperty]
    private bool _isCompareVisible;

    [ObservableProperty]
    private bool _isWorkspaceVisible = true;

    [ObservableProperty]
    private bool _isEditorVisible = true;

    [ObservableProperty]
    private bool _isPlanVisible;

    [ObservableProperty]
    private bool _isPlanTreeVisible = true;

    [ObservableProperty]
    private bool _isPlanFlamegraphVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _newConnectionName = string.Empty;

    [ObservableProperty]
    private DatabaseType _newConnectionDatabaseType = DatabaseType.PostgreSql;

    [ObservableProperty]
    private string _newConnectionHost = "localhost";

    [ObservableProperty]
    private string _newConnectionPort = "5432";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _newConnectionDatabase = string.Empty;

    [ObservableProperty]
    private string _newConnectionUsername = string.Empty;

    [ObservableProperty]
    private string _newConnectionPassword = string.Empty;

    [ObservableProperty]
    private bool _newConnectionUseSsl;

    [ObservableProperty]
    private string _connectionStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplainQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplainAnalyzeQueryCommand))]
    private ConnectionListItemViewModel? _selectedConnection;

    [ObservableProperty]
    private string _selectedConnectionPassword = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplainQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplainAnalyzeQueryCommand))]
    private string _sqlText = "select 1;";

    [ObservableProperty]
    private string _queryStatusMessage = "No query has been executed yet.";

    [ObservableProperty]
    private string _resultHeaderText = string.Empty;

    [ObservableProperty]
    private string _planSummaryText = "Run Explain to inspect the selected query plan.";

    [ObservableProperty]
    private bool _hasPlanIssues;

    [ObservableProperty]
    private string _planIssuesBadgeText = string.Empty;

    [ObservableProperty]
    private string _planIssuesBadgeColor = "#91A0AD";

    [ObservableProperty]
    private string _planTreeHeaderText = "Plan Tree";

    [ObservableProperty]
    private double _graphWidth = 800;

    [ObservableProperty]
    private double _graphHeight = 600;

    [ObservableProperty]
    private double _graphZoom = 1.0;

    [ObservableProperty]
    private string _selectedPlanNodeTitle = "No plan node selected.";

    [ObservableProperty]
    private string _selectedPlanNodeDetails = "Run Explain and select a node to inspect details.";

    public MainWindowViewModel()
        : this(
            new DesignConnectionService(),
            new DesignQueryExecutionService(),
            new DesignExplainAnalyzeService(),
            new DesignHistoryService())
    {
    }

    public MainWindowViewModel(
        IConnectionService connectionService,
        IQueryExecutionService queryExecutionService,
        IExplainAnalyzeService explainAnalyzeService,
        IHistoryService historyService)
    {
        _connectionService = connectionService;
        _queryExecutionService = queryExecutionService;
        _explainAnalyzeService = explainAnalyzeService;
        _historyService = historyService;

        NavigationItems =
        [
            new("ED", "Editor",  "Query execution workspace",
                "M2,2 H10 L14,6 V14 H2 Z M10,2 V6 H14 M4,8 H10 M4,10 H12 M4,12 H8"),
            new("PL", "Plan",    "Execution plan tree",
                "M7,0 A1.5,1.5,0,1,0,9,0 A1.5,1.5,0,1,0,7,0 M8,1.5 V6 M8,6 L4,9 M8,6 L12,9 M3,9 A1.5,1.5,0,1,0,5,9 A1.5,1.5,0,1,0,3,9 M11,9 A1.5,1.5,0,1,0,13,9 A1.5,1.5,0,1,0,11,9"),
            new("CP", "Compare", "Query comparison",
                "M2,5 H11 L9,3 M9,7 L11,5 M14,11 H5 L7,9 M7,13 L5,11"),
            new("HS", "History", "Executed query history",
                "M8,1 A7,7,0,1,0,8,15 A7,7,0,1,0,8,1 M8,4 V8 L11,10"),
            new("DB", "Connect", "Database connections",
                "M2,4 A6,2,0,1,0,14,4 M2,4 L2,12 A6,2,0,0,0,14,12 L14,4 M2,8 A6,2,0,0,0,14,8"),
        ];

        _selectedNavigationItem = NavigationItems[0];
        NavigationItems[0].IsActive = true;

        DatabaseTypeOptions = new ObservableCollection<DatabaseType>
        {
            DatabaseType.PostgreSql,
            DatabaseType.MySql,
            DatabaseType.MariaDb,
            DatabaseType.SQLite,
            DatabaseType.SqlServer
        };
    }

    public ObservableCollection<ShellNavigationItemViewModel> NavigationItems { get; }

    public ObservableCollection<DatabaseType> DatabaseTypeOptions { get; }

    public ObservableCollection<ConnectionListItemViewModel> Connections { get; } = [];

    public ObservableCollection<string> ResultColumns { get; } = [];

    public ObservableCollection<QueryResultRowViewModel> ResultRows { get; } = [];

    public ObservableCollection<PlanNodeVisualItemViewModel> VisualPlanNodes { get; } = [];

    public ObservableCollection<GraphEdgeViewModel> GraphEdges { get; } = [];

    public ObservableCollection<PlanIssueItemViewModel> PlanIssues { get; } = [];

    public ObservableCollection<PlanIssueItemViewModel> SelectedPlanNodeIssues { get; } = [];

    public ObservableCollection<QueryHistoryItemViewModel> HistoryItems { get; } = [];

    public string ActiveTitle => SelectedNavigationItem.Label;

    public string ActiveSubtitle => SelectedNavigationItem.Description;

    partial void OnSelectedNavigationItemChanged(ShellNavigationItemViewModel value)
    {
        OnPropertyChanged(nameof(ActiveTitle));
        OnPropertyChanged(nameof(ActiveSubtitle));

        foreach (var item in NavigationItems)
            item.IsActive = false;
        value.IsActive = true;

        IsConnectionsVisible = value.Code == "DB";
        IsHistoryVisible = value.Code == "HS";
        IsCompareVisible = value.Code == "CP";
        IsWorkspaceVisible = !IsConnectionsVisible && !IsHistoryVisible && !IsCompareVisible;
        IsPlanVisible = value.Code == "PL";
        IsEditorVisible = IsWorkspaceVisible && !IsPlanVisible;

        if (IsConnectionsVisible)
        {
            LoadConnectionsCommand.Execute(null);
        }

        if (IsHistoryVisible)
        {
            LoadHistoryCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void SelectNavigationItem(ShellNavigationItemViewModel navigationItem)
    {
        SelectedNavigationItem = navigationItem;
    }

    [RelayCommand]
    private void ZoomIn()  => GraphZoom = Math.Min(GraphZoom + 0.2, 2.0);

    [RelayCommand]
    private void ZoomOut() => GraphZoom = Math.Max(GraphZoom - 0.2, 0.4);

    [RelayCommand]
    private void ZoomFit() => GraphZoom = 1.0;

    [RelayCommand]
    private void SelectConnection(ConnectionListItemViewModel item)
    {
        SelectedConnection = item;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "ED");
    }

    [RelayCommand]
    private void RequestDeleteConnection(ConnectionListItemViewModel item) =>
        item.IsPendingDelete = true;

    [RelayCommand]
    private void CancelDeleteConnection(ConnectionListItemViewModel item) =>
        item.IsPendingDelete = false;

    [RelayCommand]
    private async Task DeleteConnectionAsync(ConnectionListItemViewModel item)
    {
        item.IsPendingDelete = false;
        await _connectionService.DeleteConnectionAsync(item.Id);
        Connections.Remove(item);

        if (SelectedConnection?.Id == item.Id)
            SelectedConnection = Connections.FirstOrDefault();

        ConnectionStatusMessage = $"Deleted \"{item.Name}\".";
    }

    [RelayCommand]
    public async Task LoadConnectionsAsync()
    {
        var connections = await _connectionService.GetConnectionsAsync();

        Connections.Clear();

        foreach (var connection in connections)
        {
            Connections.Add(ConnectionListItemViewModel.FromConnection(connection));
        }

        SelectedConnection ??= Connections.FirstOrDefault();

        ConnectionStatusMessage = Connections.Count == 0
            ? "No saved connections yet."
            : $"{Connections.Count} saved connection(s).";
    }

    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        var history = await _historyService.GetRecentAsync();

        HistoryItems.Clear();

        foreach (var entry in history)
        {
            HistoryItems.Add(QueryHistoryItemViewModel.FromEntry(entry));
        }
    }

    [RelayCommand(CanExecute = nameof(CanSaveConnection))]
    private async Task SaveConnectionAsync()
    {
        if (!TryParsePort(out var port))
        {
            ConnectionStatusMessage = "Port must be a number.";
            return;
        }

        var connection = await _connectionService.CreateConnectionAsync(new CreateConnectionRequest
        {
            Name = NewConnectionName,
            DatabaseType = NewConnectionDatabaseType,
            Host = NewConnectionHost,
            Port = port,
            Database = NewConnectionDatabase,
            Username = NewConnectionUsername,
            Password = null,
            UseSsl = NewConnectionUseSsl
        });

        var connectionItem = ConnectionListItemViewModel.FromConnection(connection);
        Connections.Add(connectionItem);
        SelectedConnection = connectionItem;
        SelectedConnectionPassword = NewConnectionPassword;
        ClearConnectionForm();

        ConnectionStatusMessage = $"Saved connection \"{connection.Name}\".";
    }

    [RelayCommand(CanExecute = nameof(CanSaveConnection))]
    private async Task TestConnectionAsync()
    {
        if (!TryParsePort(out var port))
        {
            ConnectionStatusMessage = "Port must be a number.";
            return;
        }

        ConnectionStatusMessage = "Testing connection...";

        var result = await _connectionService.TestConnectionAsync(new CreateConnectionRequest
        {
            Name = NewConnectionName,
            DatabaseType = NewConnectionDatabaseType,
            Host = NewConnectionHost,
            Port = port,
            Database = NewConnectionDatabase,
            Username = NewConnectionUsername,
            Password = NewConnectionPassword,
            UseSsl = NewConnectionUseSsl
        });

        ConnectionStatusMessage = result.Message;

        if (result.Succeeded)
        {
            SelectedConnectionPassword = NewConnectionPassword;
        }
    }

    private bool CanSaveConnection()
    {
        return !string.IsNullOrWhiteSpace(NewConnectionName)
            && !string.IsNullOrWhiteSpace(NewConnectionDatabase);
    }

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
        ResultRows.Clear();
        VisualPlanNodes.Clear();
        PlanIssues.Clear();
        SelectedPlanNodeIssues.Clear();
        ResultHeaderText = string.Empty;
        PlanTreeHeaderText = "Plan Tree";
        SelectedPlanNodeTitle = "No plan node selected.";
        SelectedPlanNodeDetails = "Run Explain and select a node to inspect details.";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _queryExecutionService.ExecuteAsync(CreateExecutableConnection(), SqlText);
            stopwatch.Stop();

            foreach (var col in result.Columns)
                ResultColumns.Add(col);

            BuildAlignedResultTable(result.Columns, result.Rows, ResultRows, out var header);
            ResultHeaderText = header;

            QueryStatusMessage = $"Returned {result.RowCount} row(s) in {result.Duration.TotalMilliseconds:N0} ms.";
            await RecordHistoryAsync(true, result.Duration, result.RowCount, null);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            QueryStatusMessage = GetFriendlyErrorMessage(exception);
            await RecordHistoryAsync(false, stopwatch.Elapsed, null, exception.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRunQuery))]
    private async Task ExplainQueryAsync()
    {
        await ExplainQueryCoreAsync(
            "Running explain...",
            "Explain is running...",
            (connection, sql) => _explainAnalyzeService.ExplainAsync(connection, sql),
            "Explain");
    }

    [RelayCommand(CanExecute = nameof(CanRunQuery))]
    private async Task ExplainAnalyzeQueryAsync()
    {
        await ExplainQueryCoreAsync(
            "Running explain analyze...",
            "Explain Analyze is running. The database will execute the query.",
            (connection, sql) => _explainAnalyzeService.ExplainAnalyzeAsync(connection, sql),
            "Explain Analyze");
    }

    private async Task ExplainQueryCoreAsync(
        string runningStatus,
        string runningSummary,
        Func<Connection, string, Task<ExecutionPlan>> explainFunc,
        string label)
    {
        if (SelectedConnection is null)
        {
            QueryStatusMessage = "Select a connection first.";
            return;
        }

        QueryStatusMessage = runningStatus;
        ResultColumns.Clear();
        ResultRows.Clear();
        VisualPlanNodes.Clear();
        GraphEdges.Clear();
        PlanIssues.Clear();
        SelectedPlanNodeIssues.Clear();
        ResultHeaderText = string.Empty;
        PlanSummaryText = runningSummary;
        HasPlanIssues = false;
        PlanIssuesBadgeText = string.Empty;
        SelectedPlanNodeTitle = "No plan node selected.";
        SelectedPlanNodeDetails = "Explain is running.";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var plan = await explainFunc(CreateExecutableConnection(), SqlText);
            stopwatch.Stop();

            var flattenedNodes = FlattenPlan(plan.Root).ToList();
            IReadOnlyList<string> columns = ["depth", "operation", "cost", "estimated_rows", "actual_time_ms", "actual_rows"];
            foreach (var col in columns)
                ResultColumns.Add(col);
            var issueItemsByNodeId = BuildIssueIndex(plan.Issues);

            var planRows = flattenedNodes.Select(item => (IReadOnlyDictionary<string, object?>)new Dictionary<string, object?>
            {
                ["depth"] = item.Depth,
                ["operation"] = $"{new string(' ', item.Depth * 2)}{item.Node.Label}",
                ["cost"] = item.Node.TotalCost,
                ["estimated_rows"] = item.Node.EstimatedRows,
                ["actual_time_ms"] = item.Node.ActualTotalTimeMs,
                ["actual_rows"] = item.Node.ActualRows
            }).ToList();

            BuildAlignedResultTable(columns, planRows, ResultRows, out var planHeader);
            ResultHeaderText = planHeader;

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
            {
                PlanIssues.Add(PlanIssueItemViewModel.FromIssue(issue));
            }

            PlanSummaryText = BuildPlanSummary(plan.Root, flattenedNodes.Count, plan.Issues.Count);
            UpdateIssuesBadge(plan.Issues);
            PlanTreeHeaderText = $"Plan Graph ({flattenedNodes.Count} node(s))";

            if (plan.Root is not null)
                ApplyGraphLayout(plan.Root);
            SelectPlanNode(VisualPlanNodes.FirstOrDefault());
            QueryStatusMessage = $"{label} returned {flattenedNodes.Count} plan node(s), {plan.Issues.Count} issue(s) in {stopwatch.Elapsed.TotalMilliseconds:N0} ms.";
            SelectedNavigationItem = NavigationItems.First(item => item.Code == "PL");
            await RecordHistoryAsync(true, stopwatch.Elapsed, flattenedNodes.Count, null, plan.RawJson);
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
    }

    [RelayCommand]
    private void SelectPlanNode(PlanNodeVisualItemViewModel? node)
    {
        foreach (var visualNode in VisualPlanNodes)
        {
            visualNode.IsSelected = ReferenceEquals(visualNode, node);
        }

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
        {
            SelectedPlanNodeIssues.Add(issue);
        }
    }

    [RelayCommand]
    private void ShowPlanTree()
    {
        IsPlanTreeVisible = true;
        IsPlanFlamegraphVisible = false;
    }

    [RelayCommand]
    private void ShowPlanFlamegraph()
    {
        IsPlanTreeVisible = false;
        IsPlanFlamegraphVisible = true;
    }

    private bool CanRunQuery()
    {
        return SelectedConnection is not null && !string.IsNullOrWhiteSpace(SqlText);
    }

    partial void OnSelectedConnectionChanged(ConnectionListItemViewModel? value)
    {
        SelectedConnectionPassword = string.Empty;
    }

    [RelayCommand]
    private void OpenHistoryItem(QueryHistoryItemViewModel item)
    {
        SqlText = item.SqlText;
        SelectedNavigationItem = NavigationItems.First(item => item.Code == "ED");
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
            SqlText = SqlText,
            Duration = duration,
            RowCount = rowCount,
            Succeeded = succeeded,
            ErrorMessage = errorMessage,
            ExplainJson = explainJson
        });

        HistoryItems.Insert(0, QueryHistoryItemViewModel.FromEntry(entry));
    }

    private static string GetFriendlyErrorMessage(Exception exception)
    {
        var msg = exception.Message;

        if (msg.Contains("password authentication failed", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Access denied for user", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Login failed", StringComparison.OrdinalIgnoreCase))
            return "Authentication failed. Check the username and password for this connection.";

        if (msg.Contains("No such host", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Network is unreachable", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("nodename nor servname", StringComparison.OrdinalIgnoreCase))
            return "Cannot reach the database server. Check the host and port, and ensure the server is running.";

        if (msg.Contains("SSL", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("certificate", StringComparison.OrdinalIgnoreCase))
            return "SSL connection failed. Try changing the SSL setting for this connection.";

        if (msg.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return "The query timed out. The database may be under load, or the query needs optimization.";

        if (msg.Contains("syntax error", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("You have an error in your SQL", StringComparison.OrdinalIgnoreCase))
            return $"SQL syntax error: {msg.Split('\n')[0].Trim()}";

        if (msg.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Unknown column", StringComparison.OrdinalIgnoreCase) ||
            msg.Contains("Unknown table", StringComparison.OrdinalIgnoreCase))
            return $"Object not found: {msg.Split('\n')[0].Trim()}";

        return $"Query failed: {msg.Split('\n')[0].Trim()}";
    }

    private Connection CreateExecutableConnection()
    {
        var connection = SelectedConnection?.Connection;

        if (connection is null)
        {
            throw new InvalidOperationException("Select a connection first.");
        }

        return new Connection
        {
            Id = connection.Id,
            Name = connection.Name,
            DatabaseType = connection.DatabaseType,
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            Username = connection.Username,
            Password = string.IsNullOrWhiteSpace(SelectedConnectionPassword) ? null : SelectedConnectionPassword,
            UseSsl = connection.UseSsl,
            CreatedAt = connection.CreatedAt,
            LastUsedAt = connection.LastUsedAt
        };
    }

    private static IEnumerable<(PlanNode Node, int Depth)> FlattenPlan(PlanNode? root)
    {
        if (root is null)
        {
            yield break;
        }

        foreach (var item in FlattenPlan(root, 0))
        {
            yield return item;
        }
    }

    private static IEnumerable<(PlanNode Node, int Depth)> FlattenPlan(PlanNode node, int depth)
    {
        yield return (node, depth);

        foreach (var child in node.Children)
        {
            foreach (var item in FlattenPlan(child, depth + 1))
            {
                yield return item;
            }
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
        GraphZoom   = 1.0;
    }

    private void BuildEdges(PlanNode node, IReadOnlyDictionary<Guid, (double X, double Y)> positions)
    {
        if (!positions.TryGetValue(node.Id, out var parentPos))
            return;

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
        {
            return "Explain returned no plan nodes.";
        }

        var cost = root.TotalCost is null ? "n/a" : root.TotalCost.Value.ToString("N2");
        var rows = root.EstimatedRows is null ? "n/a" : root.EstimatedRows.Value.ToString("N0");
        var actualTime = root.ActualTotalTimeMs is null ? "n/a" : $"{root.ActualTotalTimeMs.Value:N2} ms";

        return $"Root: {root.Label}\nNodes: {nodeCount}\nIssues: {issueCount}\nEstimated cost: {cost}\nEstimated rows: {rows}\nActual time: {actualTime}";
    }

    private static void BuildAlignedResultTable(
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        System.Collections.ObjectModel.ObservableCollection<QueryResultRowViewModel> target,
        out string headerText)
    {
        if (columns.Count == 0)
        {
            headerText = "No columns returned";
            return;
        }

        var formatted = rows.Select(row =>
            columns.Select(col => FormatCellValue(row.GetValueOrDefault(col))).ToArray()
        ).ToList();

        var widths = columns.Select((col, i) =>
            Math.Max(col.Length, formatted.Count == 0 ? 0 : formatted.Max(r => r[i].Length))
        ).ToArray();

        headerText = string.Join("  ", columns.Select((col, i) => col.PadRight(widths[i])));

        foreach (var (raw, fmtRow) in rows.Zip(formatted))
        {
            var displayText = string.Join("  ", fmtRow.Select((val, i) => val.PadRight(widths[i])));
            target.Add(new QueryResultRowViewModel { Values = raw, DisplayText = displayText });
        }
    }

    private static string FormatCellValue(object? value) => value switch
    {
        null => "NULL",
        DateTime dt => dt.ToString("u"),
        DateTimeOffset dto => dto.ToString("u"),
        decimal d => d.ToString("N2"),
        double d => d.ToString("N2"),
        _ => value.ToString() ?? string.Empty,
    };

    private static Dictionary<Guid, IReadOnlyList<PlanIssueItemViewModel>> BuildIssueIndex(IReadOnlyList<PlanIssue> issues)
    {
        var result = new Dictionary<Guid, List<PlanIssueItemViewModel>>();

        foreach (var issue in issues)
        {
            if (issue.PlanNodeId is null)
            {
                continue;
            }

            if (!result.TryGetValue(issue.PlanNodeId.Value, out var nodeIssues))
            {
                nodeIssues = [];
                result[issue.PlanNodeId.Value] = nodeIssues;
            }

            nodeIssues.Add(PlanIssueItemViewModel.FromIssue(issue));
        }

        return result.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<PlanIssueItemViewModel>)pair.Value);
    }

    private void ClearConnectionForm()
    {
        NewConnectionName = string.Empty;
        NewConnectionDatabaseType = DatabaseType.PostgreSql;
        NewConnectionHost = "localhost";
        NewConnectionPort = "5432";
        NewConnectionDatabase = string.Empty;
        NewConnectionUsername = string.Empty;
        NewConnectionPassword = string.Empty;
        NewConnectionUseSsl = false;
    }

    private bool TryParsePort(out int? port)
    {
        port = null;

        if (string.IsNullOrWhiteSpace(NewConnectionPort))
        {
            return true;
        }

        if (!int.TryParse(NewConnectionPort, out var parsedPort))
        {
            return false;
        }

        port = parsedPort;
        return true;
    }

    private sealed class DesignConnectionService : IConnectionService
    {
        public Task<IReadOnlyList<Connection>> GetConnectionsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Connection> connections =
            [
                new()
                {
                    Name = "Sample PostgreSQL",
                    DatabaseType = DatabaseType.PostgreSql,
                    Host = "localhost",
                    Port = 5432,
                    Database = "app_db",
                    Username = "postgres"
                }
            ];

            return Task.FromResult(connections);
        }

        public Task<Connection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Connection?>(null);
        }

        public Task<Connection> CreateConnectionAsync(
            CreateConnectionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Connection
            {
                Name = request.Name,
                DatabaseType = request.DatabaseType,
                Host = request.Host,
                Port = request.Port,
                Database = request.Database,
                Username = request.Username,
                Password = request.Password,
                UseSsl = request.UseSsl
            });
        }

        public Task<Connection?> UpdateConnectionAsync(
            Guid id,
            UpdateConnectionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Connection?>(null);
        }

        public Task<bool> DeleteConnectionAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<ConnectionTestResult> TestConnectionAsync(
            CreateConnectionRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ConnectionTestResult.Success("Design-time connection test succeeded."));
        }
    }

    private sealed class DesignQueryExecutionService : IQueryExecutionService
    {
        public Task<QueryResult> ExecuteAsync(
            Connection connection,
            string sql,
            CancellationToken cancellationToken = default)
        {
            QueryResult result = new()
            {
                Duration = TimeSpan.FromMilliseconds(8),
                RowCount = 1,
                Columns = ["value"],
                Rows =
                [
                    new Dictionary<string, object?>
                    {
                        ["value"] = 1
                    }
                ]
            };

            return Task.FromResult(result);
        }
    }

    private sealed class DesignExplainAnalyzeService : IExplainAnalyzeService
    {
        public Task<ExecutionPlan> ExplainAsync(
            Connection connection,
            string sql,
            CancellationToken cancellationToken = default)
        {
            return ExplainAnalyzeAsync(connection, sql, cancellationToken);
        }

        public Task<ExecutionPlan> ExplainAnalyzeAsync(
            Connection connection,
            string sql,
            CancellationToken cancellationToken = default)
        {
            ExecutionPlan plan = new()
            {
                Root = new PlanNode
                {
                    NodeType = NodeType.SeqScan,
                    Label = "Seq Scan on sample_table",
                    TotalCost = 12.25m,
                    EstimatedRows = 12500
                },
                Issues =
                [
                    new PlanIssue
                    {
                        Code = "SEQ_SCAN_LARGE_TABLE",
                        Severity = IssueSeverity.Critical,
                        Title = "Sequential scan on a large row set",
                        Description = "Seq Scan on sample_table scans about 12,500 row(s).",
                        Recommendation = "Check WHERE and JOIN predicates and add an index."
                    }
                ],
                RawJson = "{}"
            };

            return Task.FromResult(plan);
        }
    }

    private sealed class DesignHistoryService : IHistoryService
    {
        public Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(
            int limit = 100,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<QueryHistoryEntry> entries =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    ConnectionName = "Sample PostgreSQL",
                    SqlText = "select 1;",
                    ExecutedAt = DateTimeOffset.UtcNow,
                    Duration = TimeSpan.FromMilliseconds(8),
                    RowCount = 1,
                    Status = "success"
                }
            ];

            return Task.FromResult(entries);
        }

        public Task<QueryHistoryEntry> RecordAsync(
            RecordQueryHistoryRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new QueryHistoryEntry
            {
                Id = Guid.NewGuid(),
                ConnectionId = request.ConnectionId,
                ConnectionName = "Sample PostgreSQL",
                SqlText = request.SqlText,
                ExecutedAt = DateTimeOffset.UtcNow,
                Duration = request.Duration,
                RowCount = request.RowCount,
                Status = request.Succeeded ? "success" : "error",
                ErrorMessage = request.ErrorMessage
            });
        }
    }
}
