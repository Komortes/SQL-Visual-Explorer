using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public interface IExplainAnalyzeService
{
    Task<ExecutionPlan> ExplainAsync(Connection connection, string sql, CancellationToken cancellationToken = default);

    Task<ExecutionPlan> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default);
}
