using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Drivers;
using SQLVisualExplorer.Infrastructure.Services;

namespace SQLVisualExplorer.Infrastructure.Tests;

public sealed class QueryExecutionServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Throws_WhenSqlIsEmpty()
    {
        var service = new QueryExecutionService([]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExecuteAsync(CreateConnection(DatabaseType.PostgreSql), " "));
    }

    [Fact]
    public async Task ExecuteAsync_Throws_WhenDatabaseTypeIsUnsupported()
    {
        var service = new QueryExecutionService([]);

        await Assert.ThrowsAsync<NotSupportedException>(() =>
            service.ExecuteAsync(CreateConnection(DatabaseType.SQLite), "select 1"));
    }

    [Fact]
    public async Task ExecuteAsync_UsesMatchingDriver()
    {
        var driver = new StubDatabaseDriver(DatabaseType.MySql);
        var service = new QueryExecutionService([driver]);

        var result = await service.ExecuteAsync(CreateConnection(DatabaseType.MySql), "select 1");

        Assert.Equal(1, result.RowCount);
        Assert.True(driver.ExecuteWasCalled);
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

    private sealed class StubDatabaseDriver(DatabaseType databaseType) : IDatabaseDriver
    {
        public bool ExecuteWasCalled { get; private set; }

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
            ExecuteWasCalled = true;

            QueryResult result = new()
            {
                RowCount = 1,
                Columns = ["value"],
                Rows =
                [
                    new Dictionary<string, object?>
                    {
                        ["value"] = 1
                    }
                ]
            };

            return Task.FromResult(result);
        }

        public Task<string> ExplainAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> ExplainAnalyzeAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
