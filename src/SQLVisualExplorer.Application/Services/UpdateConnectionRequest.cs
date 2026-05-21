using SQLVisualExplorer.Domain.Enums;

namespace SQLVisualExplorer.Application.Services;

public sealed class UpdateConnectionRequest
{
    public string Name { get; init; } = string.Empty;

    public DatabaseType DatabaseType { get; init; }

    public string? Host { get; init; }

    public int? Port { get; init; }

    public string Database { get; init; } = string.Empty;

    public string? Username { get; init; }

    public bool UseSsl { get; init; }
}
