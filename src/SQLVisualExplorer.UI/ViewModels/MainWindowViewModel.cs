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
    private string _planTreeHeaderText = "Plan Tree";

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
            new("ED", "Editor", "Query execution workspace"),
            new("PL", "Plan", "Execution plan tree"),
            new("CP", "Compare", "Query comparison"),
            new("HS", "History", "Executed query history"),
            new("DB", "Connect", "Database connections")
        ];

        _selectedNavigationItem = NavigationItems[0];

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

    public ObservableCollection<QueryResultRowViewModel> ResultRows { get; } = [];

    public ObservableCollection<PlanNodeVisualItemViewModel> VisualPlanNodes { get; } = [];

    public ObservableCollection<PlanIssueItemViewModel> PlanIssues { get; } = [];

    public ObservableCollection<PlanIssueItemViewModel> SelectedPlanNodeIssues { get; } = [];

    public ObservableCollection<QueryHistoryItemViewModel> HistoryItems { get; } = [];

    public string ActiveTitle => SelectedNavigationItem.Label;

    public string ActiveSubtitle => SelectedNavigationItem.Description;

    partial void OnSelectedNavigationItemChanged(ShellNavigationItemViewModel value)
    {
        OnPropertyChanged(nameof(ActiveTitle));
        OnPropertyChanged(nameof(ActiveSubtitle));

        IsConnectionsVisible = value.Code == "DB";
        IsHistoryVisible = value.Code == "HS";
        IsWorkspaceVisible = !IsConnectionsVisible && !IsHistoryVisible;
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
            var columns = result.Columns;

            ResultHeaderText = columns.Count == 0
                ? "No columns returned"
                : string.Join("    ", columns);

            foreach (var row in result.Rows)
            {
                ResultRows.Add(QueryResultRowViewModel.FromValues(row, columns));
            }

            QueryStatusMessage = $"Returned {result.RowCount} row(s) in {result.Duration.TotalMilliseconds:N0} ms.";
            await RecordHistoryAsync(true, result.Duration, result.RowCount, null);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            QueryStatusMessage = exception.Message;
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
        ResultRows.Clear();
        VisualPlanNodes.Clear();
        PlanIssues.Clear();
        SelectedPlanNodeIssues.Clear();
        ResultHeaderText = string.Empty;
        PlanSummaryText = runningSummary;
        SelectedPlanNodeTitle = "No plan node selected.";
        SelectedPlanNodeDetails = "Explain is running.";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var plan = await explainFunc(CreateExecutableConnection(), SqlText);
            stopwatch.Stop();

            var flattenedNodes = FlattenPlan(plan.Root).ToList();
            var columns = new[] { "depth", "operation", "cost", "estimated_rows", "actual_time_ms", "actual_rows" };
            ResultHeaderText = string.Join("    ", columns);
            var issueItemsByNodeId = BuildIssueIndex(plan.Issues);

            foreach (var item in flattenedNodes)
            {
            VisualPlanNodes.Add(PlanNodeVisualItemViewModel.FromNode(
                    item.Node,
                    item.Depth,
                    plan.Root?.TotalCost,
                    plan.Root?.ActualTotalTimeMs,
                    issueItemsByNodeId.GetValueOrDefault(item.Node.Id) ?? []));

                ResultRows.Add(QueryResultRowViewModel.FromValues(
                    new Dictionary<string, object?>
                    {
                        ["depth"] = item.Depth,
                        ["operation"] = $"{new string(' ', item.Depth * 2)}{item.Node.Label}",
                        ["cost"] = item.Node.TotalCost,
                        ["estimated_rows"] = item.Node.EstimatedRows,
                        ["actual_time_ms"] = item.Node.ActualTotalTimeMs,
                        ["actual_rows"] = item.Node.ActualRows
                    },
                    columns));
            }

            foreach (var issue in plan.Issues)
            {
                PlanIssues.Add(PlanIssueItemViewModel.FromIssue(issue));
            }

            PlanSummaryText = BuildPlanSummary(plan.Root, flattenedNodes.Count, plan.Issues.Count);
            PlanTreeHeaderText = $"Plan Tree ({flattenedNodes.Count} node(s))";
            SelectPlanNode(VisualPlanNodes.FirstOrDefault());
            QueryStatusMessage = $"{label} returned {flattenedNodes.Count} plan node(s), {plan.Issues.Count} issue(s) in {stopwatch.Elapsed.TotalMilliseconds:N0} ms.";
            SelectedNavigationItem = NavigationItems.First(item => item.Code == "PL");
            await RecordHistoryAsync(true, stopwatch.Elapsed, flattenedNodes.Count, null, plan.RawJson);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            QueryStatusMessage = exception.Message;
            PlanSummaryText = $"{label} failed.";
            PlanTreeHeaderText = "Plan Tree";
            SelectedPlanNodeTitle = "No plan node selected.";
            SelectedPlanNodeDetails = exception.Message;
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
