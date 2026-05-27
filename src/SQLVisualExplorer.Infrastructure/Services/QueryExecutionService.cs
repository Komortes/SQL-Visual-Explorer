using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Drivers;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class QueryExecutionService(IEnumerable<IDatabaseDriver> drivers) : IQueryExecutionService
{
    public Task<QueryResult> ExecuteAsync(
        Connection connection,
        string sql,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL text is required.", nameof(sql));
        }

        var driver = drivers.FirstOrDefault(candidate => candidate.Supports(connection.DatabaseType));

        if (driver is null)
        {
            throw new NotSupportedException($"{connection.DatabaseType} query execution is not supported yet.");
        }

        return driver.ExecuteAsync(connection, sql, cancellationToken);
    }
}
