using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Parsers;

public sealed class SQLiteExplainParser : IExplainParser
{
    public bool Supports(DatabaseType databaseType) => databaseType == DatabaseType.SQLite;

    public ExecutionPlan Parse(string explainOutput)
    {
        var rows = ParseRows(explainOutput);

        if (rows.Count == 0)
        {
            return new ExecutionPlan
            {
                Root    = new PlanNode { NodeType = NodeType.Unknown, Label = "No plan returned" },
                RawJson = explainOutput
            };
        }

        var childrenByParent = rows
            .GroupBy(row => row.Parent)
            .ToDictionary(group => group.Key, group => group.ToList());

        var ids = rows.Select(row => row.Id).ToHashSet();

        PlanNode BuildNode((int Id, int Parent, string Detail) row) => new()
        {
            NodeType = MapDetail(row.Detail),
            Label    = row.Detail,
            Children = childrenByParent.TryGetValue(row.Id, out var children)
                ? children.Select(BuildNode).ToList()
                : []
        };

        var roots = rows
            .Where(row => row.Parent == 0 || !ids.Contains(row.Parent))
            .ToList();

        var root = roots.Count == 1
            ? BuildNode(roots[0])
            : new PlanNode
            {
                NodeType = NodeType.Unknown,
                Label    = "Query Plan",
                Children = roots.Select(BuildNode).ToList()
            };

        return new ExecutionPlan
        {
            Root    = root,
            RawJson = explainOutput
        };
    }

    private static List<(int Id, int Parent, string Detail)> ParseRows(string output)
    {
        var result = new List<(int, int, string)>();

        foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|', 3);
            if (parts.Length < 3) continue;
            if (!int.TryParse(parts[0], out var id) || !int.TryParse(parts[1], out var parent)) continue;
            result.Add((id, parent, parts[2]));
        }

        return result;
    }

    private static NodeType MapDetail(string detail)
    {
        var d = detail.ToUpperInvariant();

        if (d.Contains("USING INDEX") || d.Contains("USING INTEGER PRIMARY KEY") || d.Contains("USING ROWID"))
            return NodeType.IndexScan;
        if (d.Contains("SCAN"))
            return NodeType.SeqScan;
        if (d.Contains("B-TREE FOR ORDER BY") || d.Contains("SORT"))
            return NodeType.Sort;
        if (d.Contains("GROUP BY") || d.Contains("AGGREGATE"))
            return NodeType.Aggregate;

        return NodeType.Unknown;
    }
}
