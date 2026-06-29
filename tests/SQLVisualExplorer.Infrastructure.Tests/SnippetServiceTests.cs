using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Database.Entities;
using SQLVisualExplorer.Infrastructure.Services;

namespace SQLVisualExplorer.Infrastructure.Tests;

public sealed class SnippetServiceTests
{
    [Fact]
    public async Task CreateSnippetAsync_NormalizesAndPersistsTags()
    {
        await using var fixture = await SnippetServiceFixture.CreateAsync();
        var service = new SnippetService(new AppDbContext(fixture.Options));

        var created = await service.CreateSnippetAsync(new CreateSnippetRequest
        {
            Name = "Active orders",
            SqlText = "select * from orders where status = 'active'",
            Tags = [" reporting ", "Orders", "reporting", ""]
        });

        var stored = Assert.Single(await service.GetSnippetsAsync());

        Assert.Equal(["Orders", "reporting"], created.Tags);
        Assert.Equal(["Orders", "reporting"], stored.Tags);
    }

    [Fact]
    public async Task GetSnippetsAsync_ReadsLegacyCommaSeparatedTags()
    {
        await using var fixture = await SnippetServiceFixture.CreateAsync();

        await using (var dbContext = new AppDbContext(fixture.Options))
        {
            dbContext.Snippets.Add(new SnippetEntity
            {
                Id = Guid.NewGuid(),
                Name = "Legacy",
                SqlText = "select 1",
                Tags = "ops, reporting,ops",
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var service = new SnippetService(new AppDbContext(fixture.Options));
        var stored = Assert.Single(await service.GetSnippetsAsync());

        Assert.Equal(["ops", "reporting"], stored.Tags);
    }

    private sealed class SnippetServiceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private SnippetServiceFixture(SqliteConnection connection, DbContextOptions<AppDbContext> options)
        {
            _connection = connection;
            Options = options;
        }

        public DbContextOptions<AppDbContext> Options { get; }

        public static async Task<SnippetServiceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            return new SnippetServiceFixture(connection, options);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }
    }
}
