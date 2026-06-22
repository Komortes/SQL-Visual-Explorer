using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Drivers;
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

    [Fact]
    public async Task TestConnectionAsync_ReturnsUnsupported_WhenNoDriverMatches()
    {
        await using var fixture = await ConnectionServiceFixture.CreateAsync();
        var service = fixture.CreateService();

        var result = await service.TestConnectionAsync(new CreateConnectionRequest
        {
            Name = "Local SQLite",
            DatabaseType = DatabaseType.SQLite,
            Database = "local.db"
        });

        Assert.False(result.Succeeded);
        Assert.Contains("not supported", result.Message);
    }

    [Fact]
    public async Task TestConnectionAsync_UsesMatchingDriver()
    {
        await using var fixture = await ConnectionServiceFixture.CreateAsync();
        var driver = new StubDatabaseDriver(DatabaseType.PostgreSql);
        var service = fixture.CreateService(driver);

        var result = await service.TestConnectionAsync(new CreateConnectionRequest
        {
            Name = "Local Postgres",
            DatabaseType = DatabaseType.PostgreSql,
            Host = "localhost",
            Port = 5432,
            Database = "app_db",
            Username = "postgres",
            Password = "secret"
        });

        Assert.True(result.Succeeded);
        Assert.Equal("Driver accepted connection.", result.Message);
        Assert.Equal("secret", driver.LastTestedConnection?.Password);
    }

    [Fact]
    public async Task CreateConnectionAsync_PersistsPasswordInSecretStore()
    {
        await using var fixture = await ConnectionServiceFixture.CreateAsync();
        var secretStore = new ConnectionServiceFixture.MemorySecretStore();
        var service = fixture.CreateServiceWithSecretStore(secretStore);

        var created = await service.CreateConnectionAsync(new CreateConnectionRequest
        {
            Name = "Local Postgres",
            DatabaseType = DatabaseType.PostgreSql,
            Host = "localhost",
            Port = 5432,
            Database = "app_db",
            Username = "postgres",
            Password = "secret"
        });

        var stored = await service.GetConnectionAsync(created.Id);

        Assert.NotNull(stored);
        Assert.Equal("secret", stored.Password);
        Assert.Equal("secret", await secretStore.LoadAsync($"connection/{created.Id}"));
    }

    [Fact]
    public async Task UpdateConnectionAsync_PreservesPassword_WhenPasswordUpdateIsNotRequested()
    {
        await using var fixture = await ConnectionServiceFixture.CreateAsync();
        var secretStore = new ConnectionServiceFixture.MemorySecretStore();
        var service = fixture.CreateServiceWithSecretStore(secretStore);
        var created = await service.CreateConnectionAsync(new CreateConnectionRequest
        {
            Name = "Local Postgres",
            DatabaseType = DatabaseType.PostgreSql,
            Database = "app_db",
            Password = "secret"
        });

        var updated = await service.UpdateConnectionAsync(created.Id, new UpdateConnectionRequest
        {
            Name = "Renamed Postgres",
            DatabaseType = DatabaseType.PostgreSql,
            Database = "app_db"
        });

        Assert.NotNull(updated);
        Assert.Equal("secret", updated.Password);
        Assert.Equal("secret", await secretStore.LoadAsync($"connection/{created.Id}"));
    }

    [Fact]
    public async Task UpdateConnectionAsync_DeletesPassword_WhenRequested()
    {
        await using var fixture = await ConnectionServiceFixture.CreateAsync();
        var secretStore = new ConnectionServiceFixture.MemorySecretStore();
        var service = fixture.CreateServiceWithSecretStore(secretStore);
        var created = await service.CreateConnectionAsync(new CreateConnectionRequest
        {
            Name = "Local Postgres",
            DatabaseType = DatabaseType.PostgreSql,
            Database = "app_db",
            Password = "secret"
        });

        var updated = await service.UpdateConnectionAsync(created.Id, new UpdateConnectionRequest
        {
            Name = "Local Postgres",
            DatabaseType = DatabaseType.PostgreSql,
            Database = "app_db",
            Password = string.Empty,
            UpdatePassword = true
        });

        Assert.NotNull(updated);
        Assert.Null(updated.Password);
        Assert.Null(await secretStore.LoadAsync($"connection/{created.Id}"));
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

        public ConnectionService CreateService(params IDatabaseDriver[] drivers)
        {
            return new ConnectionService(new AppDbContext(Options), drivers, new NullSecretStore());
        }

        public ConnectionService CreateServiceWithSecretStore(ISecretStore secretStore, params IDatabaseDriver[] drivers)
        {
            return new ConnectionService(new AppDbContext(Options), drivers, secretStore);
        }

        private sealed class NullSecretStore : SQLVisualExplorer.Application.Services.ISecretStore
        {
            public Task SaveAsync(string key, string secret, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
            public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        public sealed class MemorySecretStore : ISecretStore
        {
            private readonly Dictionary<string, string> _secrets = new(StringComparer.Ordinal);

            public Task SaveAsync(string key, string secret, CancellationToken cancellationToken = default)
            {
                _secrets[key] = secret;
                return Task.CompletedTask;
            }

            public Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_secrets.GetValueOrDefault(key));
            }

            public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            {
                _secrets.Remove(key);
                return Task.CompletedTask;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }

    private sealed class StubDatabaseDriver(DatabaseType databaseType) : IDatabaseDriver
    {
        public Connection? LastTestedConnection { get; private set; }

        public bool Supports(DatabaseType candidate)
        {
            return candidate == databaseType;
        }

        public Task<ConnectionTestResult> TestConnectionAsync(
            Connection connection,
            CancellationToken cancellationToken = default)
        {
            LastTestedConnection = connection;

            return Task.FromResult(ConnectionTestResult.Success("Driver accepted connection."));
        }

        public Task<QueryResult> ExecuteAsync(Connection connection, string sql, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
