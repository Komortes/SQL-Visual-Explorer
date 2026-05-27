using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Drivers;
using SQLVisualExplorer.Infrastructure.Services;

namespace SQLVisualExplorer.Infrastructure.Tests;

public sealed class ExplainAnalyzeServiceTests
{
    [Fact]
    public async Task ExplainAnalyzeAsync_Throws_WhenSqlIsEmpty()
    {
        var service = new ExplainAnalyzeService([], new StubPlanParserService(), new StubPlanAnalyzerService());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExplainAnalyzeAsync(CreateConnection(DatabaseType.MySql), " "));
    }

    [Fact]
    public async Task ExplainAnalyzeAsync_Throws_WhenDatabaseTypeIsUnsupported()
    {
        var service = new ExplainAnalyzeService([], new StubPlanParserService(), new StubPlanAnalyzerService());

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            service.ExplainAnalyzeAsync(CreateConnection(DatabaseType.SQLite), "select 1"));
    }

    [Fact]
    public async Task ExplainAnalyzeAsync_UsesMatchingDriverAndParser()
    {
        var driver = new StubDatabaseDriver(DatabaseType.MySql);
        var parser = new StubPlanParserService();
        var analyzer = new StubPlanAnalyzerService();
        var service = new ExplainAnalyzeService([driver], parser, analyzer);

        var plan = await service.ExplainAnalyzeAsync(CreateConnection(DatabaseType.MySql), "select 1");

        Assert.True(driver.ExplainAnalyzeWasCalled);
        Assert.Equal(DatabaseType.MySql, parser.DatabaseType);
        Assert.Equal("""{"query_block":{}}""", parser.ExplainOutput);
        Assert.Equal("Query Block", plan.Root?.Label);
        Assert.True(analyzer.AnalyzeWasCalled);
        Assert.Single(plan.Issues);
    }

    [Theory]
    [InlineData("insert into users(name) values ('a')")]
    [InlineData("update users set name = 'a'")]
    [InlineData("delete from users")]
    [InlineData("drop table users")]
    [InlineData("select 1; delete from users")]
    public async Task ExplainAnalyzeAsync_BlocksUnsafeSql(string sql)
    {
        var driver = new StubDatabaseDriver(DatabaseType.MySql);
        var service = new ExplainAnalyzeService([driver], new StubPlanParserService(), new StubPlanAnalyzerService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExplainAnalyzeAsync(CreateConnection(DatabaseType.MySql), sql));

        Assert.False(driver.ExplainAnalyzeWasCalled);
    }

    [Fact]
    public async Task ExplainAnalyzeAsync_AllowsLeadingCommentsAndSelect()
    {
        var driver = new StubDatabaseDriver(DatabaseType.MySql);
        var service = new ExplainAnalyzeService([driver], new StubPlanParserService(), new StubPlanAnalyzerService());

        await service.ExplainAnalyzeAsync(CreateConnection(DatabaseType.MySql), "-- comment\nselect 1;");

        Assert.True(driver.ExplainAnalyzeWasCalled);
    }

    [Fact]
    public async Task ExplainAsync_UsesSafeExplainDriverMethod()
    {
        var driver = new StubDatabaseDriver(DatabaseType.MySql);
        var service = new ExplainAnalyzeService([driver], new StubPlanParserService(), new StubPlanAnalyzerService());

        await service.ExplainAsync(CreateConnection(DatabaseType.MySql), "select 1");

        Assert.True(driver.ExplainWasCalled);
        Assert.False(driver.ExplainAnalyzeWasCalled);
    }

    private static Connection CreateConnection(DatabaseType databaseType)
    {
        return new Connection
        {
            DatabaseType = databaseType,
            Host = "localhost",
            Database = "app_db"
        };
    }

    private sealed class StubPlanParserService : IPlanParserService
    {
        public DatabaseType? DatabaseType { get; private set; }

        public string? ExplainOutput { get; private set; }

        public ExecutionPlan Parse(DatabaseType databaseType, string explainOutput)
        {
            DatabaseType = databaseType;
            ExplainOutput = explainOutput;

            return new ExecutionPlan
            {
                Root = new PlanNode
                {
                    Label = "Query Block"
                },
                RawJson = explainOutput
            };
        }
    }

    private sealed class StubPlanAnalyzerService : IPlanAnalyzerService
    {
        public bool AnalyzeWasCalled { get; private set; }

        public IReadOnlyList<PlanIssue> Analyze(ExecutionPlan executionPlan)
        {
            AnalyzeWasCalled = true;

            return
            [
                new PlanIssue
                {
                    Code = "TEST_ISSUE",
                    Severity = IssueSeverity.Info,
                    Title = "Test issue"
                }
            ];
        }
    }

    private sealed class StubDatabaseDriver(DatabaseType databaseType) : IDatabaseDriver
    {
        public bool ExplainWasCalled { get; private set; }

        public bool ExplainAnalyzeWasCalled { get; private set; }

        public bool Supports(DatabaseType candidate)
        {
            return candidate == databaseType;
        }

        public Task<ConnectionTestResult> TestConnectionAsync(
            Connection connection,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ConnectionTestResult.Success("OK"));
        }

        public Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> ExplainAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
        {
            ExplainWasCalled = true;
            return Task.FromResult("""{"query_block":{}}""");
        }

        public Task<string> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
        {
            ExplainAnalyzeWasCalled = true;
            return Task.FromResult("""{"query_block":{}}""");
        }
    }
}
