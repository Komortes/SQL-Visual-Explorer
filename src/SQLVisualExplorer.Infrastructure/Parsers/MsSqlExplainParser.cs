using System.Xml.Linq;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Parsers;

public sealed class MsSqlExplainParser : IExplainParser
{
    private static readonly XNamespace Ns =
        "http://schemas.microsoft.com/sqlserver/2004/07/showplan";

    public bool Supports(DatabaseType databaseType) => databaseType == DatabaseType.SqlServer;

    public ExecutionPlan Parse(string explainOutput)
    {
        if (string.IsNullOrWhiteSpace(explainOutput))
            return EmptyPlan(explainOutput);

        try
        {
            var doc  = XDocument.Parse(explainOutput);
            var root = doc.Descendants(Ns + "RelOp").FirstOrDefault();

            return new ExecutionPlan
            {
                Root    = root is not null ? ParseRelOp(root) : EmptyNode(),
                RawJson = explainOutput,
            };
        }
        catch
        {
            return EmptyPlan(explainOutput);
        }
    }

    private static PlanNode ParseRelOp(XElement relOp)
    {
        var physicalOp = (string?)relOp.Attribute("PhysicalOp") ?? string.Empty;
        var nodeType   = MapPhysicalOp(physicalOp);

        var estimateRows = relOp.Attribute("EstimateRows") is { } er
            && double.TryParse(er.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var erVal)
            ? (long?)Math.Round(erVal) : null;

        var totalCost = relOp.Attribute("EstimatedTotalSubtreeCost") is { } cost
            && decimal.TryParse(cost.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var costVal)
            ? (decimal?)costVal : null;

        var actualMs = relOp.Attribute("ActualElapsedms") is { } elapsed
            && double.TryParse(elapsed.Value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var elapsedVal)
            ? (double?)elapsedVal : null;

        var label = BuildLabel(physicalOp, relOp);

        var children = DirectChildRelOps(relOp)
            .Select(ParseRelOp)
            .ToList();

        return new PlanNode
        {
            NodeType         = nodeType,
            Label            = label,
            TotalCost        = totalCost,
            EstimatedRows    = estimateRows,
            ActualTotalTimeMs = actualMs,
            Children         = children,
        };
    }

    private static string BuildLabel(string physicalOp, XElement relOp)
    {
        var obj = relOp.Descendants(Ns + "Object").FirstOrDefault();
        if (obj is null) return physicalOp;

        var schema = (string?)obj.Attribute("Schema");
        var table  = (string?)obj.Attribute("Table");
        var index  = (string?)obj.Attribute("Index");

        if (string.IsNullOrEmpty(table)) return physicalOp;

        var target = string.IsNullOrEmpty(schema) ? table : $"{schema}.{table}";
        return string.IsNullOrEmpty(index)
            ? $"{physicalOp} on {target}"
            : $"{physicalOp} on {target} ({index})";
    }

    // Only direct-child RelOp elements — not nested ones.
    private static IEnumerable<XElement> DirectChildRelOps(XElement parent)
    {
        foreach (var descendant in parent.Descendants(Ns + "RelOp"))
        {
            var ancestor = descendant
                .Ancestors(Ns + "RelOp")
                .FirstOrDefault();
            if (ancestor == parent)
                yield return descendant;
        }
    }

    private static NodeType MapPhysicalOp(string op) =>
        op.ToUpperInvariant() switch
        {
            "CLUSTERED INDEX SCAN" or "TABLE SCAN"                         => NodeType.SeqScan,
            "CLUSTERED INDEX SEEK" or "INDEX SEEK"
                or "NONCLUSTERED INDEX SEEK"                                => NodeType.IndexScan,
            "NONCLUSTERED INDEX SCAN" or "INDEX SCAN"                      => NodeType.IndexScan,
            "HASH MATCH"                                                    => NodeType.HashJoin,
            "NESTED LOOPS"                                                  => NodeType.NestedLoop,
            "MERGE JOIN"                                                    => NodeType.MergeJoin,
            "SORT"                                                          => NodeType.Sort,
            "AGGREGATE" or "HASH AGGREGATE" or "STREAM AGGREGATE"          => NodeType.Aggregate,
            _                                                               => NodeType.Unknown,
        };

    private static PlanNode EmptyNode() => new()
    {
        NodeType = NodeType.Unknown,
        Label    = "No plan available",
    };

    private static ExecutionPlan EmptyPlan(string raw) => new()
    {
        Root    = EmptyNode(),
        RawJson = raw,
    };
}
