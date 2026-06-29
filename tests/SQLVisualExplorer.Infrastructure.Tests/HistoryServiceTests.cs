using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Database.Entities;
using SQLVisualExplorer.Infrastructure.Services;

namespace SQLVisualExplorer.Infrastructure.Tests;

public sealed class HistoryServiceTests
{
    [Fact]
    public async Task RecordAsync_PersistsSuccessfulQuery()
    {
        await using var fixture = await HistoryServiceFixture.CreateAsync();
        var service = fixture.CreateService();

        var entry = await service.RecordAsync(new RecordQueryHistoryRequest
        {
            SqlText = "select 1;",
            Duration = TimeSpan.FromMilliseconds(12),
            RowCount = 1,
            Succeeded = true
        });

        var recent = await service.GetRecentAsync();

        var stored = Assert.Single(recent);
        Assert.Equal(entry.Id, stored.Id);
        Assert.Equal("select 1;", stored.SqlText);
        Assert.Equal("success", stored.Status);
        Assert.Equal(1, stored.RowCount);
        Assert.Equal(12, stored.Duration?.TotalMilliseconds);
    }

    [Fact]
    public async Task RecordAsync_PersistsFailedQuery()
    {
        await using var fixture = await HistoryServiceFixture.CreateAsync();
        var service = fixture.CreateService();

        await service.RecordAsync(new RecordQueryHistoryRequest
        {
            SqlText = "select broken",
            Duration = TimeSpan.FromMilliseconds(5),
            Succeeded = false,
            ErrorMessage = "syntax error"
        });

        var recent = await service.GetRecentAsync();

        var stored = Assert.Single(recent);
        Assert.Equal("error", stored.Status);
        Assert.Equal("syntax error", stored.ErrorMessage);
    }

    [Fact]
    public async Task RecordAsync_ReturnsSavedPlanAndConnectionDatabaseType()
    {
        await using var fixture = await HistoryServiceFixture.CreateAsync();
        var connectionId = Guid.NewGuid();

        await using (var dbContext = new AppDbContext(fixture.Options))
        {
            dbContext.Connections.Add(new ConnectionEntity
            {
                Id = connectionId,
                Name = "Local MySQL",
                DatabaseType = DatabaseType.MySql.ToString(),
                Database = "app_db",
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var service = fixture.CreateService();
        await service.RecordAsync(new RecordQueryHistoryRequest
        {
            ConnectionId = connectionId,
            DatabaseType = DatabaseType.MySql,
            SqlText = "select * from users",
            Succeeded = true,
            ExplainJson = "{\"query_block\":{}}"
        });

        var stored = Assert.Single(await service.GetRecentAsync());

        Assert.Equal(DatabaseType.MySql, stored.DatabaseType);
        Assert.Equal("{\"query_block\":{}}", stored.ExplainJson);
    }

    private sealed class HistoryServiceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private HistoryServiceFixture(SqliteConnection connection, DbContextOptions<AppDbContext> options)
        {
            _connection = connection;
            Options = options;
        }

        public DbContextOptions<AppDbContext> Options { get; }

        public static async Task<HistoryServiceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new HistoryServiceFixture(connection, options);
        }

        public HistoryService CreateService()
        {
            return new HistoryService(new AppDbContext(Options));
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
