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
    private readonly ISnippetService _snippetService;
    private readonly IQueryAdvisorService? _advisorService;
    private readonly ISecretStore? _secretStore;
    private IReadOnlyList<PlanIssue> _currentPlanIssues = [];

    [ObservableProperty]
    private ShellNavigationItemViewModel _selectedNavigationItem;

    [ObservableProperty]
    private bool _isConnectionsVisible;

    [ObservableProperty]
    private bool _isHistoryVisible;

    [ObservableProperty]
    private bool _isCompareVisible;

    [ObservableProperty]
    private bool _isSnippetsVisible;

    [ObservableProperty]
    private bool _isWorkspaceVisible = true;

    [ObservableProperty]
    private bool _isEditorVisible = true;

    [ObservableProperty]
    private bool _isPlanVisible;

    [ObservableProperty]
    private bool _isPlanTableVisible = true;

    [ObservableProperty]
    private bool _isPlanGraphVisible;

    [ObservableProperty]
    private bool _isPlanFlamegraphVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _newConnectionName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocalDatabaseConnection))]
    [NotifyPropertyChangedFor(nameof(NewConnectionDatabaseLabel))]
    [NotifyPropertyChangedFor(nameof(NewConnectionDatabaseWatermark))]
    private DatabaseType _newConnectionDatabaseType = DatabaseType.PostgreSql;

    public bool IsLocalDatabaseConnection => NewConnectionDatabaseType == DatabaseType.SQLite;

    public string NewConnectionDatabaseLabel => IsLocalDatabaseConnection ? "File path" : "Database";

    public string NewConnectionDatabaseWatermark => IsLocalDatabaseConnection
        ? "/path/to/database.sqlite"
        : "app_db";

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
    [NotifyCanExecuteChangedFor(nameof(RunCompareCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(RunQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplainQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExplainAnalyzeQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunCompareCommand))]
    private bool _isBusy;

    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _resultHeaderText = string.Empty;

    [ObservableProperty]
    private double _resultTotalWidth;

    private readonly List<QueryHistoryItemViewModel> _allHistoryItems = [];

    [ObservableProperty]
    private string _historyFilterText = string.Empty;

    [ObservableProperty]
    private bool _historyShowSlowOnly;

    [ObservableProperty]
    private int _historySlowThresholdMs = 500;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSnippetsPopupCommand))]
    private bool _isSnippetsPopupVisible;

    [ObservableProperty]
    private string _snippetsPopupSearchText = string.Empty;

    private readonly List<QueryResultRowViewModel> _resultRowsBacking = [];

    private string? _resultSortColumn;

    private bool _resultSortAscending = true;

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

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSnippetCommand))]
    private string _newSnippetName = string.Empty;

    [ObservableProperty]
    private string _newSnippetDescription = string.Empty;

    [ObservableProperty]
    private string _newSnippetSqlText = string.Empty;

    [ObservableProperty]
    private string _snippetStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCompareCommand))]
    private string _compareQueryAText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCompareCommand))]
    private string _compareQueryBText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCompareCommand))]
    private bool _isCompareRunning;

    [ObservableProperty]
    private string _compareStatusMessage = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportCompareToHtmlCommand))]
    private bool _compareHasResults;

    [ObservableProperty]
    private string _advisorApiKey = string.Empty;

    [ObservableProperty]
    private string _advisorEndpoint = string.Empty;

    [ObservableProperty]
    private string _advisorModel = string.Empty;

    [ObservableProperty]
    private bool _isAdvisorSettingsVisible;

    [ObservableProperty]
    private string _advisorOutput = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunAdvisorCommand))]
    private bool _isAdvisorRunning;

    [ObservableProperty]
    private bool _isAdvisorPanelVisible;

    [ObservableProperty]
    private ComparePlanResultViewModel? _compareResultA;

    [ObservableProperty]
    private ComparePlanResultViewModel? _compareResultB;

    public ObservableCollection<PlanNodeVisualItemViewModel> ComparePlanNodesA { get; } = [];
    public ObservableCollection<PlanNodeVisualItemViewModel> ComparePlanNodesB { get; } = [];

    public Func<string, Task<string?>>? RequestSaveFilePath { get; set; }

    public Func<double>? ComputeFitZoom { get; set; }

    public Func<string, Task>? CopyTextToClipboard { get; set; }

    public MainWindowViewModel()
        : this(
            new DesignConnectionService(),
            new DesignQueryExecutionService(),
            new DesignExplainAnalyzeService(),
            new DesignHistoryService(),
            new DesignSnippetService())
    {
    }

    public MainWindowViewModel(
        IConnectionService connectionService,
        IQueryExecutionService queryExecutionService,
        IExplainAnalyzeService explainAnalyzeService,
        IHistoryService historyService,
        ISnippetService snippetService,
        IQueryAdvisorService? advisorService = null,
        ISecretStore? secretStore = null)
    {
        _connectionService = connectionService;
        _queryExecutionService = queryExecutionService;
        _explainAnalyzeService = explainAnalyzeService;
        _historyService = historyService;
        _snippetService = snippetService;
        _advisorService = advisorService;
        _secretStore = secretStore;

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
            new("SN", "Snippets", "Saved SQL snippets",
                "M2,2 H14 V6 H2 Z M2,8 H14 V12 H2 Z M4,4 H12 M4,10 H9"),
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

    public ObservableCollection<ResultColumnViewModel> ResultColumnsView { get; } = [];

    public ObservableCollection<PlanNodeVisualItemViewModel> VisualPlanNodes { get; } = [];

    public ObservableCollection<GraphEdgeViewModel> GraphEdges { get; } = [];

    public ObservableCollection<PlanIssueItemViewModel> PlanIssues { get; } = [];

    public ObservableCollection<PlanIssueItemViewModel> SelectedPlanNodeIssues { get; } = [];

    public ObservableCollection<QueryHistoryItemViewModel> HistoryItems { get; } = [];

    public ObservableCollection<SnippetItemViewModel> Snippets { get; } = [];

    public ObservableCollection<SnippetItemViewModel> PopupSnippets { get; } = [];

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
        IsSnippetsVisible = value.Code == "SN";
        IsWorkspaceVisible = !IsConnectionsVisible && !IsHistoryVisible && !IsCompareVisible && !IsSnippetsVisible;
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

        if (IsSnippetsVisible)
        {
            LoadSnippetsCommand.Execute(null);
        }

        if (IsCompareVisible && string.IsNullOrWhiteSpace(CompareQueryAText))
        {
            CompareQueryAText = SqlText;
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
    private void ZoomFit() => GraphZoom = ComputeFitZoom?.Invoke() ?? 1.0;

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

        _allHistoryItems.Clear();
        foreach (var entry in history)
            _allHistoryItems.Add(QueryHistoryItemViewModel.FromEntry(entry));

        ApplyHistoryFilters();
    }

    [RelayCommand]
    private void ApplyHistoryFilters()
    {
        var filtered = _allHistoryItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(HistoryFilterText))
        {
            var term = HistoryFilterText.Trim();
            filtered = filtered.Where(item =>
                item.SqlPreview.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.ConnectionName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (HistoryShowSlowOnly)
            filtered = filtered.Where(item =>
                item.DurationMs.HasValue && item.DurationMs.Value >= HistorySlowThresholdMs);

        HistoryItems.Clear();
        foreach (var item in filtered)
            HistoryItems.Add(item);
    }

    [RelayCommand]
    private void ClearHistoryFilters()
    {
        HistoryFilterText = string.Empty;
        HistoryShowSlowOnly = false;
        HistorySlowThresholdMs = 500;
        ApplyHistoryFilters();
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
        ResultColumnsView.Clear();
        ResultRows.Clear();
        VisualPlanNodes.Clear();
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
            var result = await _queryExecutionService.ExecuteAsync(CreateExecutableConnection(), SqlText, _cts.Token);
            stopwatch.Stop();

            BuildResultGrid(result.Columns, result.Rows);
            ExportResultsToCsvCommand.NotifyCanExecuteChanged();

            QueryStatusMessage = $"Returned {result.RowCount} row(s) in {result.Duration.TotalMilliseconds:N0} ms.";
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
            {
                PlanIssues.Add(PlanIssueItemViewModel.FromIssue(issue));
            }

            _currentPlanIssues = plan.Issues;
            PlanSummaryText = BuildPlanSummary(plan.Root, flattenedNodes.Count, plan.Issues.Count);
            UpdateIssuesBadge(plan.Issues);
            PlanTreeHeaderText = $"Plan ({flattenedNodes.Count} node(s))";

            if (plan.Root is not null)
                ApplyGraphLayout(plan.Root);
            SelectPlanNode(VisualPlanNodes.FirstOrDefault());
            ExportResultsToCsvCommand.NotifyCanExecuteChanged();
            QueryStatusMessage = $"{label} returned {flattenedNodes.Count} plan node(s), {plan.Issues.Count} issue(s) in {stopwatch.Elapsed.TotalMilliseconds:N0} ms.";
            SelectedNavigationItem = NavigationItems.First(item => item.Code == "PL");
            await RecordHistoryAsync(true, stopwatch.Elapsed, flattenedNodes.Count, null, plan.RawJson);
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

    private bool CanRunQuery()
    {
        return SelectedConnection is not null && !string.IsNullOrWhiteSpace(SqlText) && !IsBusy;
    }

    [RelayCommand]
    private void CancelRun() => _cts?.Cancel();

    [RelayCommand]
    private void ShowAbout()
    {
        QueryStatusMessage = "SQL Visual Explorer — analyze EXPLAIN plans fast. Tree, graph and grid views.";
    }

    partial void OnSelectedConnectionChanged(ConnectionListItemViewModel? value)
    {
        SelectedConnectionPassword = value?.Connection?.Password ?? string.Empty;
    }

    [RelayCommand]
    private void OpenHistoryItem(QueryHistoryItemViewModel item)
    {
        SqlText = item.SqlText;
        SelectedNavigationItem = NavigationItems.First(item => item.Code == "ED");
    }

    [RelayCommand]
    private void CopyHistoryItemSql(QueryHistoryItemViewModel item)
    {
        if (CopyTextToClipboard is not null)
            _ = CopyTextToClipboard(item.SqlText);
    }

    [RelayCommand]
    private void RequestDeleteHistoryItem(QueryHistoryItemViewModel item) =>
        item.IsPendingDelete = true;

    [RelayCommand]
    private void CancelDeleteHistoryItem(QueryHistoryItemViewModel item) =>
        item.IsPendingDelete = false;

    [RelayCommand]
    private async Task DeleteHistoryItemAsync(QueryHistoryItemViewModel item)
    {
        item.IsPendingDelete = false;
        await _historyService.DeleteAsync(item.Id);
        HistoryItems.Remove(item);
    }

    [RelayCommand]
    private void AddHistoryItemToSnippets(QueryHistoryItemViewModel item)
    {
        NewSnippetName = string.Empty;
        NewSnippetDescription = string.Empty;
        NewSnippetSqlText = item.SqlText;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "SN");
    }

    [RelayCommand]
    public async Task LoadSnippetsAsync()
    {
        var snippets = await _snippetService.GetSnippetsAsync();

        Snippets.Clear();

        foreach (var snippet in snippets)
            Snippets.Add(SnippetItemViewModel.FromSnippet(snippet));

        SnippetStatusMessage = Snippets.Count == 0
            ? "No saved snippets yet."
            : $"{Snippets.Count} snippet(s).";
    }

    [RelayCommand(CanExecute = nameof(CanSaveSnippet))]
    private async Task SaveSnippetAsync()
    {
        var snippet = await _snippetService.CreateSnippetAsync(new CreateSnippetRequest
        {
            Name = NewSnippetName,
            Description = NewSnippetDescription,
            SqlText = NewSnippetSqlText
        });

        Snippets.Insert(0, SnippetItemViewModel.FromSnippet(snippet));
        NewSnippetName = string.Empty;
        NewSnippetDescription = string.Empty;
        NewSnippetSqlText = string.Empty;
        SnippetStatusMessage = $"Saved snippet \"{snippet.Name}\".";
    }

    private bool CanSaveSnippet() => !string.IsNullOrWhiteSpace(NewSnippetName);

    [RelayCommand]
    private void OpenSnippet(SnippetItemViewModel item)
    {
        SqlText = item.SqlText;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "ED");
    }

    [RelayCommand]
    private async Task OpenSnippetsPopupAsync()
    {
        if (Snippets.Count == 0)
            await LoadSnippetsAsync();
        SnippetsPopupSearchText = string.Empty;
        RefreshPopupSnippets();
        IsSnippetsPopupVisible = true;
    }

    [RelayCommand(CanExecute = nameof(IsSnippetsPopupVisible))]
    private void CloseSnippetsPopup() => IsSnippetsPopupVisible = false;

    [RelayCommand]
    private void OpenSnippetFromPopup(SnippetItemViewModel item)
    {
        SqlText = item.SqlText;
        IsSnippetsPopupVisible = false;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "ED");
    }

    partial void OnSnippetsPopupSearchTextChanged(string value) => RefreshPopupSnippets();

    private void RefreshPopupSnippets()
    {
        PopupSnippets.Clear();
        var term = SnippetsPopupSearchText.Trim();
        foreach (var snippet in Snippets)
        {
            if (string.IsNullOrEmpty(term) ||
                snippet.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                snippet.SqlPreview.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                PopupSnippets.Add(snippet);
            }
        }
    }

    [RelayCommand]
    private void SaveCurrentQueryAsSnippet()
    {
        NewSnippetName = string.Empty;
        NewSnippetDescription = string.Empty;
        NewSnippetSqlText = SqlText;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "SN");
    }

    [RelayCommand]
    private void RequestDeleteSnippet(SnippetItemViewModel item) =>
        item.IsPendingDelete = true;

    [RelayCommand]
    private void CancelDeleteSnippet(SnippetItemViewModel item) =>
        item.IsPendingDelete = false;

    [RelayCommand]
    private async Task DeleteSnippetAsync(SnippetItemViewModel item)
    {
        item.IsPendingDelete = false;
        await _snippetService.DeleteSnippetAsync(item.Id);
        Snippets.Remove(item);
        SnippetStatusMessage = $"Deleted \"{item.Name}\".";
    }

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

    [RelayCommand(CanExecute = nameof(CanExportResults))]
    private async Task ExportResultsToCsvAsync()
    {
        if (RequestSaveFilePath is null) return;

        var path = await RequestSaveFilePath("results.csv");
        if (path is null) return;

        try
        {
            var columns = ResultColumns.ToList();
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(string.Join(",", columns.Select(EscapeCsvField)));

            foreach (var row in ResultRows)
            {
                lines.AppendLine(string.Join(",",
                    columns.Select(col => EscapeCsvField(FormatCellValue(row.Values.GetValueOrDefault(col))))));
            }

            await File.WriteAllTextAsync(path, lines.ToString(), System.Text.Encoding.UTF8);
            QueryStatusMessage = $"Exported {ResultRows.Count} row(s) to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            QueryStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportResults))]
    private async Task ExportResultsToJsonAsync()
    {
        if (RequestSaveFilePath is null) return;

        var path = await RequestSaveFilePath("results.json");
        if (path is null) return;

        try
        {
            var columns = ResultColumns.ToList();
            var rows = ResultRows
                .Select(row => columns.ToDictionary(
                    col => col,
                    col => (object?)FormatCellValue(row.Values.GetValueOrDefault(col))))
                .ToList();

            var json = System.Text.Json.JsonSerializer.Serialize(
                rows,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(path, json, System.Text.Encoding.UTF8);
            QueryStatusMessage = $"Exported {ResultRows.Count} row(s) to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            QueryStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportPlan))]
    private async Task ExportPlanToHtmlAsync()
    {
        if (RequestSaveFilePath is null) return;
        var path = await RequestSaveFilePath("plan.html");
        if (path is null) return;
        try
        {
            var html = PlanHtmlExporter.Generate(SqlText, VisualPlanNodes, PlanIssues, PlanSummaryText);
            await File.WriteAllTextAsync(path, html, System.Text.Encoding.UTF8);
            QueryStatusMessage = $"Plan exported to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            QueryStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private bool CanExportPlan() => VisualPlanNodes.Count > 0;

    [RelayCommand(CanExecute = nameof(CanExportCompare))]
    private async Task ExportCompareToHtmlAsync()
    {
        if (RequestSaveFilePath is null) return;
        var path = await RequestSaveFilePath("compare.html");
        if (path is null) return;
        try
        {
            var html = CompareHtmlExporter.Generate(
                CompareQueryAText, CompareQueryBText,
                CompareResultA, CompareResultB,
                ComparePlanNodesA, ComparePlanNodesB);
            await File.WriteAllTextAsync(path, html, System.Text.Encoding.UTF8);
            CompareStatusMessage = $"Report saved to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            CompareStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private bool CanExportCompare() => CompareHasResults;

    [RelayCommand]
    private void FormatSql()
    {
        if (!string.IsNullOrWhiteSpace(SqlText))
            SqlText = SqlFormatter.Format(SqlText);
    }

    private static void PopulateComparePlanNodes(
        ObservableCollection<PlanNodeVisualItemViewModel> target,
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

    private bool CanExportResults() => ResultRows.Count > 0;

    private static string EscapeCsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
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
        GraphZoom   = ComputeFitZoom?.Invoke() ?? 1.0;
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

    private void BuildResultGrid(
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        ResultColumns.Clear();
        ResultColumnsView.Clear();
        ResultRows.Clear();
        _resultRowsBacking.Clear();
        _resultSortColumn = null;
        _resultSortAscending = true;

        if (columns.Count == 0)
        {
            ResultTotalWidth = 0;
            return;
        }

        var widths = columns.ToDictionary(
            col => col,
            col =>
            {
                var maxContent = rows.Count == 0
                    ? 0
                    : rows.Max(row => FormatCellValue(row.GetValueOrDefault(col)).Length);
                var chars = Math.Max(col.Length, maxContent);
                return Math.Clamp(chars * 7.5 + 22, 72, 360);
            });

        foreach (var col in columns)
        {
            ResultColumns.Add(col);
            ResultColumnsView.Add(new ResultColumnViewModel { Name = col, Width = widths[col] });
        }

        ResultTotalWidth = widths.Values.Sum();

        foreach (var row in rows)
        {
            _resultRowsBacking.Add(new QueryResultRowViewModel
            {
                Values = row,
                Cells = columns.Select(col =>
                {
                    var value = row.GetValueOrDefault(col);
                    return new ResultCellViewModel
                    {
                        Text = FormatCellValue(value),
                        Width = widths[col],
                        IsNull = value is null
                    };
                }).ToList()
            });
        }

        foreach (var row in _resultRowsBacking)
            ResultRows.Add(row);
    }

    [RelayCommand]
    private void SortResultsByColumn(string columnName)
    {
        if (string.IsNullOrEmpty(columnName) || _resultRowsBacking.Count == 0)
            return;

        if (_resultSortColumn == columnName)
            _resultSortAscending = !_resultSortAscending;
        else
        {
            _resultSortColumn = columnName;
            _resultSortAscending = true;
        }

        _resultRowsBacking.Sort((a, b) =>
        {
            var left = a.Values.GetValueOrDefault(columnName);
            var right = b.Values.GetValueOrDefault(columnName);
            if (left is null && right is null) return 0;
            if (left is null) return 1;
            if (right is null) return -1;
            var comparison = CompareNonNull(left, right);
            return _resultSortAscending ? comparison : -comparison;
        });

        ResultRows.Clear();
        foreach (var row in _resultRowsBacking)
            ResultRows.Add(row);

        var glyph = _resultSortAscending ? "▲" : "▼";
        var rebuilt = ResultColumnsView
            .Select(col => new ResultColumnViewModel
            {
                Name = col.Name,
                Width = col.Width,
                SortGlyph = col.Name == columnName ? glyph : string.Empty
            })
            .ToList();
        ResultColumnsView.Clear();
        foreach (var col in rebuilt)
            ResultColumnsView.Add(col);
    }

    [RelayCommand]
    private void CopySelectedResultRow(QueryResultRowViewModel? row)
    {
        if (row is null || CopyTextToClipboard is null)
            return;

        var line = string.Join(
            "\t",
            ResultColumns.Select(col => FormatCellValue(row.Values.GetValueOrDefault(col))));
        _ = CopyTextToClipboard(line);
    }

    private static int CompareNonNull(object left, object right)
    {
        if (left.GetType() == right.GetType() && left is IComparable comparable)
            return comparable.CompareTo(right);

        var leftText = left.ToString() ?? string.Empty;
        var rightText = right.ToString() ?? string.Empty;
        if (double.TryParse(leftText, out var leftNumber) && double.TryParse(rightText, out var rightNumber))
            return leftNumber.CompareTo(rightNumber);

        return string.Compare(leftText, rightText, StringComparison.OrdinalIgnoreCase);
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

        if (IsLocalDatabaseConnection || string.IsNullOrWhiteSpace(NewConnectionPort))
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

    [RelayCommand]
    private void OpenAdvisorSettings() => IsAdvisorSettingsVisible = true;

    [RelayCommand]
    private void CloseAdvisorSettings() => IsAdvisorSettingsVisible = false;

    [RelayCommand]
    private async Task SaveAdvisorSettingsAsync()
    {
        if (_secretStore is null) return;
        await _secretStore.SaveAsync("advisor-api-key", AdvisorApiKey);
        await _secretStore.SaveAsync("advisor-endpoint", AdvisorEndpoint);
        await _secretStore.SaveAsync("advisor-model", AdvisorModel);
        IsAdvisorSettingsVisible = false;
        RunAdvisorCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task LoadAdvisorSettingsAsync()
    {
        if (_secretStore is null) return;
        AdvisorApiKey    = await _secretStore.LoadAsync("advisor-api-key")    ?? string.Empty;
        AdvisorEndpoint  = await _secretStore.LoadAsync("advisor-endpoint")   ?? string.Empty;
        AdvisorModel     = await _secretStore.LoadAsync("advisor-model")      ?? string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanRunAdvisor))]
    private async Task RunAdvisorAsync()
    {
        if (_advisorService is null) return;
        IsAdvisorRunning = true;
        AdvisorOutput    = string.Empty;
        try
        {
            var dbType = SelectedConnection?.DatabaseType.ToString() ?? "Unknown";
            var result = await _advisorService.AnalyzeAsync(SqlText, dbType, _currentPlanIssues);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(result.Summary);
            if (result.Suggestions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Suggestions:");
                foreach (var s in result.Suggestions)
                    sb.AppendLine($"- {s}");
            }
            if (!string.IsNullOrWhiteSpace(result.RewrittenSql))
            {
                sb.AppendLine();
                sb.AppendLine("Rewritten SQL:");
                sb.AppendLine(result.RewrittenSql);
            }

            AdvisorOutput       = sb.ToString().TrimEnd();
            IsAdvisorPanelVisible = true;
        }
        catch (Exception ex)
        {
            AdvisorOutput = $"Error: {ex.Message}";
        }
        finally
        {
            IsAdvisorRunning = false;
        }
    }

    private bool CanRunAdvisor() =>
        !string.IsNullOrWhiteSpace(SqlText) && !IsAdvisorRunning && (_advisorService?.IsConfigured == true);

    [RelayCommand]
    private void CloseAdvisorPanel() => IsAdvisorPanelVisible = false;

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

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class DesignSnippetService : ISnippetService
    {
        public Task<IReadOnlyList<Snippet>> GetSnippetsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Snippet> snippets =
            [
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Count all orders",
                    Description = "Quick row count for the orders table",
                    SqlText = "SELECT COUNT(*) FROM orders;",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ];

            return Task.FromResult(snippets);
        }

        public Task<Snippet> CreateSnippetAsync(CreateSnippetRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Snippet
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                SqlText = request.SqlText,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        public Task<bool> DeleteSnippetAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
