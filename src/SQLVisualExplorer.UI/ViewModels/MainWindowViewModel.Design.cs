using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

// Design-time service stubs used only by the parameterless constructor (Avalonia previewer).
public sealed partial class MainWindowViewModel
{
    private sealed class DesignConnectionService : IConnectionService
    {
        public Task<IReadOnlyList<Connection>> GetConnectionsAsync(
            CancellationToken cancellationToken = default)
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

        public Task<Connection?> GetConnectionAsync(
            Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<Connection?>(null);

        public Task<Connection> CreateConnectionAsync(
            CreateConnectionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Connection
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

        public Task<Connection?> UpdateConnectionAsync(
            Guid id,
            UpdateConnectionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Connection?>(null);

        public Task<bool> DeleteConnectionAsync(
            Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<ConnectionTestResult> TestConnectionAsync(
            CreateConnectionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ConnectionTestResult.Success("Design-time connection test succeeded."));
    }

    private sealed class DesignQueryExecutionService : IQueryExecutionService
    {
        public Task<QueryResult> ExecuteAsync(
            Connection connection,
            string sql,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new QueryResult
            {
                Duration = TimeSpan.FromMilliseconds(8),
                RowCount = 1,
                Columns = ["value"],
                Rows = [new Dictionary<string, object?> { ["value"] = 1 }]
            });
    }

    private sealed class DesignExplainAnalyzeService : IExplainAnalyzeService
    {
        public Task<ExecutionPlan> ExplainAsync(
            Connection connection, string sql, CancellationToken cancellationToken = default) =>
            ExplainAnalyzeAsync(connection, sql, cancellationToken);

        public Task<ExecutionPlan> ExplainAnalyzeAsync(
            Connection connection, string sql, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ExecutionPlan
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
            });
    }

    private sealed class DesignPlanParserService : IPlanParserService
    {
        public ExecutionPlan Parse(DatabaseType databaseType, string explainOutput) =>
            new()
            {
                Root = new PlanNode
                {
                    NodeType = NodeType.Unknown,
                    Label = "Saved execution plan"
                },
                RawJson = explainOutput
            };
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
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new QueryHistoryEntry
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

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class DesignSnippetService : ISnippetService
    {
        public Task<IReadOnlyList<Snippet>> GetSnippetsAsync(
            CancellationToken cancellationToken = default)
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

        public Task<Snippet> CreateSnippetAsync(
            CreateSnippetRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Snippet
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                SqlText = request.SqlText,
                CreatedAt = DateTimeOffset.UtcNow
            });

        public Task<bool> DeleteSnippetAsync(
            Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
