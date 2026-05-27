using SQLVisualExplorer.Domain.Enums;

namespace SQLVisualExplorer.Domain.Models;

public sealed class Connection
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public DatabaseType DatabaseType { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string Database { get; init; } = string.Empty;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; init; }
}
