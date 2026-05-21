using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public interface IQueryExecutionService
{
    Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default);
}
