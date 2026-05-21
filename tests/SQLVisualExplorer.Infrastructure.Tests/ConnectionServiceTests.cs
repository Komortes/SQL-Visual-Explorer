using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Services;

namespace SQLVisualExplorer.Infrastructure.Tests;

public sealed class ConnectionServiceTests
{
    [Fact]
    public async Task CreateConnectionAsync_PersistsConnectionMetadata()
    {
        await using var fixture = await ConnectionServiceFixture.CreateAsync();
        var service = fixture.CreateService();

        var created = await service.CreateConnectionAsync(new CreateConnectionRequest
        {
            Name = "Local Postgres",
            DatabaseType = DatabaseType.PostgreSql,
            Host = " localhost ",
            Port = 5432,
            Database = " app_db ",
            Username = " postgres ",
            UseSsl = true
        });

        var connections = await service.GetConnectionsAsync();

        var connection = Assert.Single(connections);
        Assert.Equal(created.Id, connection.Id);
        Assert.Equal("Local Postgres", connection.Name);
        Assert.Equal(DatabaseType.PostgreSql, connection.DatabaseType);
        Assert.Equal("localhost", connection.Host);
        Assert.Equal(5432, connection.Port);
        Assert.Equal("app_db", connection.Database);
        Assert.Equal("postgres", connection.Username);
        Assert.True(connection.UseSsl);
    }

    [Fact]
    public async Task UpdateConnectionAsync_ReturnsNull_WhenConnectionDoesNotExist()
    {
        await using var fixture = await ConnectionServiceFixture.CreateAsync();
        var service = fixture.CreateService();

        var result = await service.UpdateConnectionAsync(Guid.NewGuid(), new UpdateConnectionRequest
        {
            Name = "Missing",
            DatabaseType = DatabaseType.MySql,
            Database = "missing"
        });

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteConnectionAsync_RemovesExistingConnection()
    {
        await using var fixture = await ConnectionServiceFixture.CreateAsync();
        var service = fixture.CreateService();

        var created = await service.CreateConnectionAsync(new CreateConnectionRequest
        {
            Name = "Local MySQL",
            DatabaseType = DatabaseType.MySql,
            Host = "localhost",
            Port = 3306,
            Database = "app_db"
        });

        var deleted = await service.DeleteConnectionAsync(created.Id);
        var connections = await service.GetConnectionsAsync();

        Assert.True(deleted);
        Assert.Empty(connections);
    }

    private sealed class ConnectionServiceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private ConnectionServiceFixture(SqliteConnection connection, DbContextOptions<AppDbContext> options)
        {
            _connection = connection;
            Options = options;
        }

        private DbContextOptions<AppDbContext> Options { get; }

        public static async Task<ConnectionServiceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new ConnectionServiceFixture(connection, options);
        }

        public ConnectionService CreateService()
        {
            return new ConnectionService(new AppDbContext(Options));
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
