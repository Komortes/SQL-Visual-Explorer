using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Application.Services;

namespace SQLVisualExplorer.Infrastructure.Drivers;

public interface IDatabaseDriver
{
    bool Supports(DatabaseType databaseType);

    Task<ConnectionTestResult> TestConnectionAsync(Connection connection, CancellationToken cancellationToken = default);

    Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default);

    Task<string> ExplainAsync(Connection connection, string sql, CancellationToken cancellationToken = default);

    Task<string> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default);
}
