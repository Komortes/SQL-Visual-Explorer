using System.Globalization;
using System.Text.Json;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Parsers;

public sealed class MySqlExplainParser : IExplainParser
{
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
        var lines = explainOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        return new ExecutionPlan
        {
            Root = new PlanNode
            {
                NodeType = NodeType.Unknown,
                Label = lines.FirstOrDefault() ?? "MySQL EXPLAIN ANALYZE",
                ActualTotalTimeMs = TryFindLastNumber(lines.FirstOrDefault()),
                Children = lines.Skip(1).Select(line => new PlanNode
                {
                    NodeType = MapTextNodeType(line),
                    Label = line,
                    ActualTotalTimeMs = TryFindLastNumber(line)
                }).ToList()
            },
            RawJson = explainOutput
        };
    }

    private static NodeType MapTextNodeType(string line)
    {
        if (line.Contains("table scan", StringComparison.OrdinalIgnoreCase))
        {
            return NodeType.SeqScan;
        }

        if (line.Contains("index", StringComparison.OrdinalIgnoreCase))
        {
            return NodeType.IndexScan;
        }

        if (line.Contains("nested loop", StringComparison.OrdinalIgnoreCase))
        {
            return NodeType.NestedLoop;
        }

        if (line.Contains("sort", StringComparison.OrdinalIgnoreCase))
        {
            return NodeType.Sort;
        }

        return NodeType.Unknown;
    }

    private static double? TryFindLastNumber(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var matches = System.Text.RegularExpressions.Regex.Matches(line, @"\d+(?:\.\d+)?");
        return matches.Count == 0
            ? null
            : double.Parse(matches[^1].Value, CultureInfo.InvariantCulture);
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
            EstimatedRows = GetLong(table, "rows_examined_per_scan") ?? GetLong(table, "rows_produced_per_join")
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
}
