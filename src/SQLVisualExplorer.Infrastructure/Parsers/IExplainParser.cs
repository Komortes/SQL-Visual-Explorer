using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Domain.Enums;

namespace SQLVisualExplorer.Infrastructure.Parsers;

public interface IExplainParser
{
    bool Supports(DatabaseType databaseType);

    ExecutionPlan Parse(string explainOutput);
}
