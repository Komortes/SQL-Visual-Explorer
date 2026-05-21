using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public interface IPlanAnalyzerService
{
    IReadOnlyList<PlanIssue> Analyze(ExecutionPlan executionPlan);
}
