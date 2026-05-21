namespace SQLVisualExplorer.Infrastructure.Database.Entities;

public sealed class ConnectionEntity
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DatabaseType { get; set; } = string.Empty;

    public string? Host { get; set; }

    public int? Port { get; set; }

    public string Database { get; set; } = string.Empty;

    public string? Username { get; set; }

    public bool UseSsl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastUsed { get; set; }

    public ICollection<QueryHistoryEntity> QueryHistory { get; } = [];
}
