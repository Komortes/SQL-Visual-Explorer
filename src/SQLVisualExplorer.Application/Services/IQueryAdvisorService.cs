using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public interface IQueryAdvisorService
{
    bool IsConfigured { get; }

    Task<AdvisorResult> AnalyzeAsync(
        string sql,
        string databaseType,
        IEnumerable<PlanIssue> issues,
        CancellationToken cancellationToken = default);
}
