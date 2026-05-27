using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Parsers;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class PlanParserService(IEnumerable<IExplainParser> parsers) : IPlanParserService
{
    public ExecutionPlan Parse(DatabaseType databaseType, string explainOutput)
    {
        if (string.IsNullOrWhiteSpace(explainOutput))
        {
            throw new ArgumentException("Explain output is required.", nameof(explainOutput));
        }

        var parser = parsers.FirstOrDefault(parser => parser.Supports(databaseType));

        if (parser is null)
        {
            throw new NotSupportedException($"{databaseType} explain output is not supported yet.");
        }

        return parser.Parse(explainOutput);
    }
}
