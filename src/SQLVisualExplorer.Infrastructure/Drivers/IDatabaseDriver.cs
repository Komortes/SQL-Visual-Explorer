using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Drivers;

public interface IDatabaseDriver
{
    Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default);
    Task<string> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default);
}
