using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Infrastructure.Parsers;
using SQLVisualExplorer.Infrastructure.Services;

namespace SQLVisualExplorer.Infrastructure.Tests;

public sealed class ExplainParserTests
{
    [Fact]
    public void PostgreSqlExplainParser_ParsesRootAndChildren()
    {
        const string explainJson = """
        [
          {
            "Plan": {
              "Node Type": "Nested Loop",
              "Total Cost": 42.5,
              "Plan Rows": 3,
              "Plans": [
                {
                  "Node Type": "Seq Scan",
                  "Relation Name": "users",
                  "Total Cost": 12.1,
                  "Plan Rows": 100
                }
              ]
            }
          }
        ]
        """;

        var parser = new PostgreSqlExplainParser();

        var plan = parser.Parse(explainJson);

        Assert.Equal(NodeType.NestedLoop, plan.Root?.NodeType);
        Assert.Equal(42.5m, plan.Root?.TotalCost);
        Assert.Equal("Seq Scan on users", plan.Root?.Children.Single().Label);
    }

    [Fact]
    public void MySqlExplainParser_ParsesTableAccess()
    {
        const string explainJson = """
        {
          "query_block": {
            "select_id": 1,
            "cost_info": {
              "query_cost": "3.10"
            },
            "table": {
              "table_name": "users",
              "access_type": "ALL",
              "rows_examined_per_scan": 25,
              "cost_info": {
                "prefix_cost": "3.10"
              }
            }
          }
        }
        """;

        var parser = new MySqlExplainParser();

        var plan = parser.Parse(explainJson);
        var tableNode = plan.Root?.Children.Single();

        Assert.Equal("Query Block", plan.Root?.Label);
        Assert.Equal(3.10m, plan.Root?.TotalCost);
        Assert.Equal(NodeType.SeqScan, tableNode?.NodeType);
        Assert.Equal("ALL on users", tableNode?.Label);
        Assert.Equal(25, tableNode?.EstimatedRows);
    }

    [Fact]
    public void MySqlExplainParser_ParsesTextExplainAnalyze()
    {
        const string explainText = """
        -> Table scan on users  (cost=3.10 rows=25) (actual time=0.035..0.041 rows=25 loops=1)
            -> Sort: users.id  (actual time=0.031..0.033 rows=25 loops=1)
        """;

        var parser = new MySqlExplainParser();

        var plan = parser.Parse(explainText);

        Assert.Equal(NodeType.Unknown, plan.Root?.NodeType);
        Assert.Contains("Table scan", plan.Root?.Label);
        Assert.Equal(NodeType.Sort, plan.Root?.Children.Single().NodeType);
        Assert.Equal(explainText, plan.RawJson);
    }

    [Fact]
    public void PlanParserService_UsesMatchingParser()
    {
        var service = new PlanParserService([new PostgreSqlExplainParser(), new MySqlExplainParser()]);

        var plan = service.Parse(DatabaseType.MySql, """{"query_block":{}}""");

        Assert.Equal("Query Block", plan.Root?.Label);
    }
}
