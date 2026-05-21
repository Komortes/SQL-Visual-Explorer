using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Infrastructure.Database.Entities;

namespace SQLVisualExplorer.Infrastructure.Database;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ConnectionEntity> Connections => Set<ConnectionEntity>();

    public DbSet<QueryHistoryEntity> QueryHistory => Set<QueryHistoryEntity>();

    public DbSet<SnippetEntity> Snippets => Set<SnippetEntity>();

    public DbSet<ComparisonEntity> Comparisons => Set<ComparisonEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureConnections(modelBuilder);
        ConfigureQueryHistory(modelBuilder);
        ConfigureSnippets(modelBuilder);
        ConfigureComparisons(modelBuilder);
    }

    private static void ConfigureConnections(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ConnectionEntity>();

        entity.ToTable("connections");
        entity.HasKey(connection => connection.Id);

        entity.Property(connection => connection.Id).HasColumnName("id");
        entity.Property(connection => connection.Name).HasColumnName("name").IsRequired();
        entity.Property(connection => connection.DatabaseType).HasColumnName("db_type").IsRequired();
        entity.Property(connection => connection.Host).HasColumnName("host");
        entity.Property(connection => connection.Port).HasColumnName("port");
        entity.Property(connection => connection.Database).HasColumnName("database").IsRequired();
        entity.Property(connection => connection.Username).HasColumnName("username");
        entity.Property(connection => connection.UseSsl).HasColumnName("use_ssl");
        entity.Property(connection => connection.CreatedAt).HasColumnName("created_at");
        entity.Property(connection => connection.LastUsed).HasColumnName("last_used");
    }

    private static void ConfigureQueryHistory(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<QueryHistoryEntity>();

        entity.ToTable("query_history");
        entity.HasKey(history => history.Id);

        entity.Property(history => history.Id).HasColumnName("id");
        entity.Property(history => history.ConnectionId).HasColumnName("connection_id");
        entity.Property(history => history.SqlText).HasColumnName("sql_text").IsRequired();
        entity.Property(history => history.ExecutedAt).HasColumnName("executed_at");
        entity.Property(history => history.DurationMs).HasColumnName("duration_ms");
        entity.Property(history => history.RowCount).HasColumnName("row_count");
        entity.Property(history => history.Status).HasColumnName("status").IsRequired();
        entity.Property(history => history.ErrorMessage).HasColumnName("error_message");
        entity.Property(history => history.ExplainJson).HasColumnName("explain_json");

        entity
            .HasOne(history => history.Connection)
            .WithMany(connection => connection.QueryHistory)
            .HasForeignKey(history => history.ConnectionId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    private static void ConfigureSnippets(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SnippetEntity>();

        entity.ToTable("snippets");
        entity.HasKey(snippet => snippet.Id);

        entity.Property(snippet => snippet.Id).HasColumnName("id");
        entity.Property(snippet => snippet.Name).HasColumnName("name").IsRequired();
        entity.Property(snippet => snippet.Description).HasColumnName("description");
        entity.Property(snippet => snippet.SqlText).HasColumnName("sql_text").IsRequired();
        entity.Property(snippet => snippet.Tags).HasColumnName("tags");
        entity.Property(snippet => snippet.CreatedAt).HasColumnName("created_at");
    }

    private static void ConfigureComparisons(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ComparisonEntity>();

        entity.ToTable("comparisons");
        entity.HasKey(comparison => comparison.Id);

        entity.Property(comparison => comparison.Id).HasColumnName("id");
        entity.Property(comparison => comparison.Name).HasColumnName("name");
        entity.Property(comparison => comparison.QueryA).HasColumnName("query_a").IsRequired();
        entity.Property(comparison => comparison.QueryB).HasColumnName("query_b").IsRequired();
        entity.Property(comparison => comparison.ResultJson).HasColumnName("result_json");
        entity.Property(comparison => comparison.CreatedAt).HasColumnName("created_at");
    }
}
