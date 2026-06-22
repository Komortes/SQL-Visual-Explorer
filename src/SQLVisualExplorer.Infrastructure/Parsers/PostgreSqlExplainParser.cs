using System.Text.Json;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Parsers;

public sealed class PostgreSqlExplainParser : IExplainParser
{
    public bool Supports(DatabaseType databaseType)
    {
        return databaseType == DatabaseType.PostgreSql;
    }

    public ExecutionPlan Parse(string explainOutput)
    {
        using var document = JsonDocument.Parse(explainOutput);
        var rootElement = document.RootElement;
        var planElement = rootElement.ValueKind == JsonValueKind.Array
            ? rootElement[0].GetProperty("Plan")
            : rootElement.GetProperty("Plan");

        return new ExecutionPlan
        {
            Root = ParseNode(planElement),
            RawJson = explainOutput
        };
    }

    private static PlanNode ParseNode(JsonElement element)
    {
        var nodeTypeName = GetString(element, "Node Type") ?? "Unknown";
        var children = new List<PlanNode>();

        if (TryGetProperty(element, "Plans", out var plans) && plans.ValueKind == JsonValueKind.Array)
        {
            children.AddRange(plans.EnumerateArray().Select(ParseNode));
        }

        return new PlanNode
        {
            NodeType = MapNodeType(nodeTypeName),
            Label = BuildLabel(element, nodeTypeName),
            TotalCost = GetDecimal(element, "Total Cost"),
            ActualTotalTimeMs = GetDouble(element, "Actual Total Time"),
            EstimatedRows = GetLong(element, "Plan Rows"),
            ActualRows = GetLong(element, "Actual Rows"),
            ActualLoops = GetLong(element, "Actual Loops"),
            RelationName = GetString(element, "Relation Name"),
            IndexName = GetString(element, "Index Name"),
            Filter = FirstNonEmpty(
                GetString(element, "Filter"),
                GetString(element, "Index Cond"),
                GetString(element, "Recheck Cond")),
            JoinCondition = JoinNonEmpty(
                GetString(element, "Hash Cond"),
                GetString(element, "Merge Cond"),
                GetString(element, "Join Filter")),
            HashBatches = GetInt(element, "Hash Batches"),
            Children = children
        };
    }

    private static string BuildLabel(JsonElement element, string nodeTypeName)
    {
        var relationName = GetString(element, "Relation Name");
        var indexName = GetString(element, "Index Name");

        if (!string.IsNullOrWhiteSpace(relationName))
        {
            return $"{nodeTypeName} on {relationName}";
        }

        if (!string.IsNullOrWhiteSpace(indexName))
        {
            return $"{nodeTypeName} using {indexName}";
        }

        return nodeTypeName;
    }

    private static NodeType MapNodeType(string nodeTypeName)
    {
        return nodeTypeName.Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase) switch
        {
            "SeqScan" => NodeType.SeqScan,
            "IndexScan" => NodeType.IndexScan,
            "IndexOnlyScan" => NodeType.IndexOnlyScan,
            "BitmapHeapScan" => NodeType.BitmapHeapScan,
            "NestedLoop" => NodeType.NestedLoop,
            "HashJoin" => NodeType.HashJoin,
            "MergeJoin" => NodeType.MergeJoin,
            "Sort" => NodeType.Sort,
            "Aggregate" => NodeType.Aggregate,
            _ => NodeType.Unknown
        };
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static decimal? GetDecimal(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property) && property.TryGetDecimal(out var value)
            ? value
            : null;
    }

    private static double? GetDouble(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property) && property.TryGetDouble(out var value)
            ? value
            : null;
    }

    private static long? GetLong(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property) && property.TryGetInt64(out var value)
            ? value
            : null;
    }

    private static int? GetInt(JsonElement element, string propertyName)
    {
        return TryGetProperty(element, propertyName, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? JoinNonEmpty(params string?[] values)
    {
        var conditions = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return conditions.Length == 0 ? null : string.Join("; ", conditions);
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        return element.TryGetProperty(propertyName, out property);
    }
}
