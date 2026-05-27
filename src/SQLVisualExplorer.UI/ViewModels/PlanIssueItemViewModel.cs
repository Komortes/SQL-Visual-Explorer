using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed class PlanIssueItemViewModel
{
    public Guid? PlanNodeId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Recommendation { get; init; } = string.Empty;

    public string SeverityColor { get; init; } = "#91A0AD";

    public static PlanIssueItemViewModel FromIssue(PlanIssue issue)
    {
        return new PlanIssueItemViewModel
        {
            PlanNodeId = issue.PlanNodeId,
            Code = issue.Code,
            Severity = issue.Severity.ToString(),
            Title = issue.Title,
            Description = issue.Description,
            Recommendation = issue.Recommendation,
            SeverityColor = issue.Severity switch
            {
                IssueSeverity.Critical => "#FF8A7A",
                IssueSeverity.Warning => "#FFD166",
                IssueSeverity.Info => "#80B8FF",
                _ => "#91A0AD"
            }
        };
    }
}
