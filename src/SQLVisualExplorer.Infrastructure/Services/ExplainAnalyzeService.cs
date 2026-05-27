using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Drivers;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class ExplainAnalyzeService(
    IEnumerable<IDatabaseDriver> drivers,
    IPlanParserService planParserService,
    IPlanAnalyzerService planAnalyzerService) : IExplainAnalyzeService
{
    public async Task<ExecutionPlan> ExplainAsync(
        Connection connection,
        string sql,
        CancellationToken cancellationToken = default)
    {
        var driver = GetDriver(connection, sql);
        var explainOutput = await driver.ExplainAsync(connection, sql, cancellationToken);

        return ParseAndAnalyze(connection, explainOutput);
    }

    public async Task<ExecutionPlan> ExplainAnalyzeAsync(
        Connection connection,
        string sql,
        CancellationToken cancellationToken = default)
    {
        SqlSafetyGuard.ThrowIfNotSafeForExplainAnalyze(sql);

        var driver = GetDriver(connection, sql);
        var explainOutput = await driver.ExplainAnalyzeAsync(connection, sql, cancellationToken);

        return ParseAndAnalyze(connection, explainOutput);
    }

    private IDatabaseDriver GetDriver(Connection connection, string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL text is required.", nameof(sql));
        }

        var driver = drivers.FirstOrDefault(driver => driver.Supports(connection.DatabaseType));

        if (driver is null)
        {
            throw new NotSupportedException($"{connection.DatabaseType} is not supported yet.");
        }

        return driver;
    }

    private ExecutionPlan ParseAndAnalyze(Connection connection, string explainOutput)
    {
        var plan = planParserService.Parse(connection.DatabaseType, explainOutput);

        return new ExecutionPlan
        {
            Id = plan.Id,
            Root = plan.Root,
            RawJson = plan.RawJson,
            Issues = planAnalyzerService.Analyze(plan)
        };
    }
}
