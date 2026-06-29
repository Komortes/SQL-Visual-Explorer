using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    // ── Service dependencies ─────────────────────────────────────────────────

    private readonly IConnectionService _connectionService;
    private readonly IQueryExecutionService _queryExecutionService;
    private readonly IExplainAnalyzeService _explainAnalyzeService;
    private readonly IPlanParserService _planParserService;
    private readonly IHistoryService _historyService;
    private readonly ISnippetService _snippetService;
    private readonly IQueryAdvisorService? _advisorService;
    private readonly ISecretStore? _secretStore;

    // ── Non-observable backing state ─────────────────────────────────────────

    private IReadOnlyList<PlanIssue> _currentPlanIssues = [];
    private CancellationTokenSource? _cts;
    private readonly List<QueryHistoryItemViewModel> _allHistoryItems = [];
    private readonly List<SnippetItemViewModel> _allSnippetItems = [];
    private readonly List<QueryResultRowViewModel> _resultRowsBacking = [];
    private string? _resultSortColumn;
    private bool _resultSortAscending = true;

    // ── Navigation ───────────────────────────────────────────────────────────

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

    // ── Connection form ──────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveConnectionCommand))]
    [NotifyCanExecuteChangedFor(nameof(TestConnectionCommand))]
    private string _newConnectionName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLocalDatabaseConnection))]
    [NotifyPropertyChangedFor(nameof(NewConnectionDatabaseLabel))]
    [NotifyPropertyChangedFor(nameof(NewConnectionDatabaseWatermark))]
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
    [NotifyPropertyChangedFor(nameof(ConnectionFormTitle))]
    [NotifyPropertyChangedFor(nameof(SaveConnectionButtonText))]
    [NotifyPropertyChangedFor(nameof(IsEditingConnection))]
    private Guid? _editingConnectionId;

    // ── Active connection / query ────────────────────────────────────────────

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

    // ── Result grid ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _resultHeaderText = string.Empty;

    [ObservableProperty]
    private double _resultTotalWidth;

    // ── History ──────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _historyFilterText = string.Empty;

    [ObservableProperty]
    private bool _historyShowSlowOnly;

    [ObservableProperty]
    private int _historySlowThresholdMs = 500;

    // ── Snippets ─────────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseSnippetsPopupCommand))]
    private bool _isSnippetsPopupVisible;

    [ObservableProperty]
    private string _snippetsPopupSearchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveSnippetCommand))]
    private string _newSnippetName = string.Empty;

    [ObservableProperty]
    private string _newSnippetDescription = string.Empty;

    [ObservableProperty]
    private string _newSnippetSqlText = string.Empty;

    [ObservableProperty]
    private string _newSnippetTags = string.Empty;

    [ObservableProperty]
    private string _snippetStatusMessage = string.Empty;

    [ObservableProperty]
    private string _snippetFilterText = string.Empty;

    // ── Execution plan ───────────────────────────────────────────────────────

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

    // ── Compare ──────────────────────────────────────────────────────────────

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
    private ComparePlanResultViewModel? _compareResultA;

    [ObservableProperty]
    private ComparePlanResultViewModel? _compareResultB;

    // ── Advisor ──────────────────────────────────────────────────────────────

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

    // ── Collections ──────────────────────────────────────────────────────────

    public ObservableCollection<ShellNavigationItemViewModel> NavigationItems { get; }

    public ObservableCollection<DatabaseType> DatabaseTypeOptions { get; }

    public ObservableCollection<ConnectionListItemViewModel> Connections { get; } = [];

    public ObservableCollection<string> ResultColumns { get; } = [];

    public ObservableCollection<QueryResultRowViewModel> ResultRows { get; } = [];

    public ObservableCollection<ResultColumnViewModel> ResultColumnsView { get; } = [];

    public ObservableCollection<PlanNodeVisualItemViewModel> VisualPlanNodes { get; } = [];

    public ObservableCollection<GraphEdgeViewModel> GraphEdges { get; } = [];

    public ObservableCollection<PlanIssueItemViewModel> PlanIssues { get; } = [];

    public ObservableCollection<PlanNodeRowViewModel> PlanTreeRoots { get; } = [];

    public ObservableCollection<PlanIssueItemViewModel> SelectedPlanNodeIssues { get; } = [];

    public ObservableCollection<QueryHistoryItemViewModel> HistoryItems { get; } = [];

    public ObservableCollection<SnippetItemViewModel> Snippets { get; } = [];

    public ObservableCollection<SnippetItemViewModel> PopupSnippets { get; } = [];

    public ObservableCollection<PlanNodeVisualItemViewModel> ComparePlanNodesA { get; } = [];

    public ObservableCollection<PlanNodeVisualItemViewModel> ComparePlanNodesB { get; } = [];

    // ── Computed properties ──────────────────────────────────────────────────

    public bool IsLocalDatabaseConnection => NewConnectionDatabaseType == DatabaseType.SQLite;

    public string NewConnectionDatabaseLabel => IsLocalDatabaseConnection ? "File path" : "Database";

    public string NewConnectionDatabaseWatermark => IsLocalDatabaseConnection
        ? "/path/to/database.sqlite"
        : "app_db";

    public bool IsEditingConnection => EditingConnectionId is not null;

    public string ConnectionFormTitle => IsEditingConnection ? "Edit Connection" : "New Connection";

    public string SaveConnectionButtonText => IsEditingConnection ? "Update Connection" : "Save Connection";

    public string ActiveTitle => SelectedNavigationItem.Label;

    public string ActiveSubtitle => SelectedNavigationItem.Description;

    // ── View callbacks (set by code-behind) ──────────────────────────────────

    public Func<string, Task<string?>>? RequestSaveFilePath { get; set; }

    public Func<double>? ComputeFitZoom { get; set; }

    public Func<string, Task>? CopyTextToClipboard { get; set; }

    // ── Constructors ─────────────────────────────────────────────────────────

    public MainWindowViewModel()
        : this(
            new DesignConnectionService(),
            new DesignQueryExecutionService(),
            new DesignExplainAnalyzeService(),
            new DesignPlanParserService(),
            new DesignHistoryService(),
            new DesignSnippetService())
    {
    }

    public MainWindowViewModel(
        IConnectionService connectionService,
        IQueryExecutionService queryExecutionService,
        IExplainAnalyzeService explainAnalyzeService,
        IPlanParserService planParserService,
        IHistoryService historyService,
        ISnippetService snippetService,
        IQueryAdvisorService? advisorService = null,
        ISecretStore? secretStore = null)
    {
        _connectionService = connectionService;
        _queryExecutionService = queryExecutionService;
        _explainAnalyzeService = explainAnalyzeService;
        _planParserService = planParserService;
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

    // ── Navigation ───────────────────────────────────────────────────────────

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
            LoadConnectionsCommand.Execute(null);

        if (IsHistoryVisible)
            LoadHistoryCommand.Execute(null);

        if (IsSnippetsVisible)
            LoadSnippetsCommand.Execute(null);

        if (IsCompareVisible && string.IsNullOrWhiteSpace(CompareQueryAText))
            CompareQueryAText = SqlText;
    }

    [RelayCommand]
    private void SelectNavigationItem(ShellNavigationItemViewModel navigationItem) =>
        SelectedNavigationItem = navigationItem;

    [RelayCommand]
    private void ZoomIn() => GraphZoom = Math.Min(GraphZoom + 0.2, 2.0);

    [RelayCommand]
    private void ZoomOut() => GraphZoom = Math.Max(GraphZoom - 0.2, 0.4);

    [RelayCommand]
    private void ZoomFit() => GraphZoom = ComputeFitZoom?.Invoke() ?? 1.0;

    // ── Shared static helpers ─────────────────────────────────────────────────

    private static IEnumerable<(PlanNode Node, int Depth)> FlattenPlan(PlanNode? root)
    {
        if (root is null) yield break;
        foreach (var item in FlattenPlan(root, 0))
            yield return item;
    }

    private static IEnumerable<(PlanNode Node, int Depth)> FlattenPlan(PlanNode node, int depth)
    {
        yield return (node, depth);
        foreach (var child in node.Children)
            foreach (var item in FlattenPlan(child, depth + 1))
                yield return item;
    }

    internal static string FormatCellValue(object? value) => value switch
    {
        null => "NULL",
        DateTime dt => dt.ToString("u"),
        DateTimeOffset dto => dto.ToString("u"),
        decimal d => d.ToString("N2"),
        double d => d.ToString("N2"),
        _ => value.ToString() ?? string.Empty,
    };

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

    private static Dictionary<Guid, IReadOnlyList<PlanIssueItemViewModel>> BuildIssueIndex(
        IReadOnlyList<PlanIssue> issues)
    {
        var result = new Dictionary<Guid, List<PlanIssueItemViewModel>>();

        foreach (var issue in issues)
        {
            if (issue.PlanNodeId is null) continue;

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
}
