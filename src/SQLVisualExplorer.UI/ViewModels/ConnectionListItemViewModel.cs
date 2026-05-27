using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed class ConnectionListItemViewModel
{
    public Guid Id { get; init; }

    public Connection Connection { get; init; } = new();

    public string Name { get; init; } = string.Empty;

    public string DatabaseType { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public static ConnectionListItemViewModel FromConnection(Connection connection)
    {
        var endpoint = connection.Port is null
            ? connection.Host ?? "local"
            : $"{connection.Host ?? "local"}:{connection.Port}";

        return new ConnectionListItemViewModel
        {
            Id = connection.Id,
            Connection = connection,
            Name = connection.Name,
            DatabaseType = connection.DatabaseType.ToString(),
            Endpoint = endpoint,
            Database = connection.Database,
            Username = string.IsNullOrWhiteSpace(connection.Username) ? "No username" : connection.Username
        };
    }
}
