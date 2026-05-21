using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Parsers;

public interface IExplainParser
{
    ExecutionPlan Parse(string explainOutput);
}
