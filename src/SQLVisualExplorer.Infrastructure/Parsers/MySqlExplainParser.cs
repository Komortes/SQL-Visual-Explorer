using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Parsers;

public sealed class MySqlExplainParser : IExplainParser
{
    private static readonly Regex ActualTimeRegex = new(
        @"actual time=(?<start>\d+(?:\.\d+)?)\.\.(?<end>\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CostRegex = new(
        @"cost=(?<start>\d+(?:\.\d+)?)\.\.(?<end>\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RowsRegex = new(
        @"rows=(?<value>\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LoopsRegex = new(
        @"loops=(?<value>\d+(?:\.\d+)?)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool Supports(DatabaseType databaseType)
    {
        return databaseType is DatabaseType.MySql or DatabaseType.MariaDb;
    }

    public ExecutionPlan Parse(string explainOutput)
    {
        if (!LooksLikeJson(explainOutput))
        {
            return ParseTextExplainAnalyze(explainOutput);
        }

        using var document = JsonDocument.Parse(explainOutput);
        var rootElement = document.RootElement;

        return new ExecutionPlan
        {
            Root = TryGetProperty(rootElement, "query_block", out var queryBlock)
                ? ParseQueryBlock(queryBlock)
                : ParseUnknownObject("Plan", rootElement),
            RawJson = explainOutput
        };
    }

    private static bool LooksLikeJson(string explainOutput)
    {
        var trimmed = explainOutput.TrimStart();
        return trimmed.StartsWith('{') || trimmed.StartsWith('[');
    }

    private static ExecutionPlan ParseTextExplainAnalyze(string explainOutput)
    {
        var roots = new List<AnalyzeNode>();
        var stack = new Stack<(int Indent, AnalyzeNode Node)>();

        foreach (var rawLine in explainOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var arrowIndex = rawLine.IndexOf("->", StringComparison.Ordinal);
            if (arrowIndex < 0)
            {
                continue;
            }

            var indent = arrowIndex;
            var node = ParseAnalyzeLine(rawLine[(arrowIndex + 2)..].Trim());

            while (stack.Count > 0 && stack.Peek().Indent >= indent)
            {
                stack.Pop();
            }

            if (stack.Count == 0)
            {
                roots.Add(node);
            }
            else
            {
                stack.Peek().Node.Children.Add(node);
            }

            stack.Push((indent, node));
        }

        var root = roots.Count switch
        {
            0 => new PlanNode { NodeType = NodeType.Unknown, Label = "MySQL EXPLAIN ANALYZE" },
            1 => ToPlanNode(roots[0]),
            _ => new PlanNode
            {
                NodeType = NodeType.Unknown,
                Label = "Query Plan",
                Children = roots.Select(ToPlanNode).ToList()
            }
        };

        return new ExecutionPlan
        {
            Root = root,
            RawJson = explainOutput
        };
    }

    private static AnalyzeNode ParseAnalyzeLine(string line)
    {
        var labelEnd = line.IndexOf("  (", StringComparison.Ordinal);
        var label = (labelEnd < 0 ? line : line[..labelEnd]).Trim();

        return new AnalyzeNode
        {
            Label = label,
            NodeType = MapTextNodeType(label),
            TotalCost = GetMetricDecimal(CostRegex, line, "end"),
            ActualTotalTimeMs = GetMetricDouble(ActualTimeRegex, line, "end"),
            ActualRows = GetMetricLong(RowsRegex, line),
            ActualLoops = GetMetricLong(LoopsRegex, line)
        };
    }

    private static PlanNode ToPlanNode(AnalyzeNode node)
    {
        return new PlanNode
        {
            NodeType = node.NodeType,
            Label = node.Label,
            TotalCost = node.TotalCost,
            ActualTotalTimeMs = node.ActualTotalTimeMs,
            ActualRows = node.ActualRows,
            ActualLoops = node.ActualLoops,
            RelationName = TryExtractRelationName(node.Label),
            Children = node.Children.Select(ToPlanNode).ToList()
        };
    }

    private static NodeType MapTextNodeType(string line)
    {
        if (line.Contains("nested loop", StringComparison.OrdinalIgnoreCase))
        {
            return NodeType.NestedLoop;
        }

        if (line.Contains("table scan", StringComparison.OrdinalIgnoreCase))
        {
            return NodeType.SeqScan;
        }

        if (line.Contains("index", StringComparison.OrdinalIgnoreCase))
        {
            return NodeType.IndexScan;
        }

        if (line.Contains("sort", StringComparison.OrdinalIgnoreCase))
        {
            return NodeType.Sort;
        }

        return NodeType.Unknown;
    }

    private static decimal? GetMetricDecimal(Regex regex, string line, string group)
    {
        var match = regex.Match(line);
        return match.Success && decimal.TryParse(match.Groups[group].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static double? GetMetricDouble(Regex regex, string line, string group)
    {
        var match = regex.Match(line);
        return match.Success && double.TryParse(match.Groups[group].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static long? GetMetricLong(Regex regex, string line)
    {
        var match = regex.Match(line);
        return match.Success && double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? (long)Math.Round(value, MidpointRounding.AwayFromZero)
            : null;
    }

    private static string? TryExtractRelationName(string label)
    {
        const string onMarker = " on ";
        var index = label.IndexOf(onMarker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? null : label[(index + onMarker.Length)..].Trim();
    }

    private static PlanNode ParseQueryBlock(JsonElement queryBlock)
    {
        var children = new List<PlanNode>();

        if (TryGetProperty(queryBlock, "nested_loop", out var nestedLoop) && nestedLoop.ValueKind == JsonValueKind.Array)
        {
            children.AddRange(nestedLoop.EnumerateArray().Select(ParseNestedLoopItem));
        }

        AddKnownChild(queryBlock, "table", children);
        AddKnownChild(queryBlock, "ordering_operation", children);
        AddKnownChild(queryBlock, "grouping_operation", children);
        AddKnownChild(queryBlock, "duplicates_removal", children);

        return new PlanNode
        {
            NodeType = NodeType.Unknown,
            Label = "Query Block",
            TotalCost = GetCost(queryBlock, "query_cost"),
            Children = children
        };
    }

    private static PlanNode ParseNestedLoopItem(JsonElement item)
    {
        if (TryGetProperty(item, "table", out var table))
        {
            return ParseTable(table);
        }

        if (TryGetProperty(item, "query_block", out var queryBlock))
        {
            return ParseQueryBlock(queryBlock);
        }

        return ParseUnknownObject("Nested Loop Item", item);
    }

    private static PlanNode ParseTable(JsonElement table)
    {
        var tableName = GetString(table, "table_name") ?? "table";
        var accessType = GetString(table, "access_type") ?? "unknown";

        return new PlanNode
        {
            NodeType = MapAccessType(accessType),
            Label = $"{accessType} on {tableName}",
            TotalCost = GetCost(table, "prefix_cost"),
            EstimatedRows = GetLong(table, "rows_examined_per_scan") ?? GetLong(table, "rows_produced_per_join"),
            RelationName = tableName,
            IndexName = GetString(table, "key"),
            Filter = FirstNonEmpty(GetString(table, "attached_condition"), GetString(table, "index_condition"))
        };
    }

    private static PlanNode ParseUnknownObject(string label, JsonElement element)
    {
        var children = new List<PlanNode>();

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (property.NameEquals("query_block"))
            {
                children.Add(ParseQueryBlock(property.Value));
                continue;
            }

            if (property.NameEquals("table"))
            {
                children.Add(ParseTable(property.Value));
            }
        }

        return new PlanNode
        {
            NodeType = NodeType.Unknown,
            Label = label,
            Children = children
        };
    }

    private static void AddKnownChild(JsonElement source, string propertyName, List<PlanNode> children)
    {
        if (!TryGetProperty(source, propertyName, out var property) || property.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        children.Add(propertyName == "table"
            ? ParseTable(property)
            : ParseUnknownObject(ToLabel(propertyName), property));
    }

    private static NodeType MapAccessType(string accessType)
    {
        return accessType.ToUpperInvariant() switch
        {
            "ALL" => NodeType.SeqScan,
            "INDEX" or "RANGE" or "REF" or "EQ_REF" or "CONST" => NodeType.IndexScan,
            _ => NodeType.Unknown
        };
    }

    private static string ToLabel(string propertyName)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(propertyName.Replace('_', ' '));
    }

    private static decimal? GetCost(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, "cost_info", out var costInfo))
        {
            return null;
        }

        return GetDecimal(costInfo, propertyName);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDecimal(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String
            && decimal.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static long? GetLong(JsonElement element, string propertyName)
    {
        if (!TryGetProperty(element, propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var number))
        {
            return number;
        }

        if (property.ValueKind == JsonValueKind.String
            && long.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private sealed class AnalyzeNode
    {
        public NodeType NodeType { get; init; }
        public string Label { get; init; } = string.Empty;
        public decimal? TotalCost { get; init; }
        public double? ActualTotalTimeMs { get; init; }
        public long? ActualRows { get; init; }
        public long? ActualLoops { get; init; }
        public List<AnalyzeNode> Children { get; } = [];
    }
}
