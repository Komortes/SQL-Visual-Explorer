using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public interface IPlanParserService
{
    ExecutionPlan Parse(DatabaseType databaseType, string explainOutput);
}
