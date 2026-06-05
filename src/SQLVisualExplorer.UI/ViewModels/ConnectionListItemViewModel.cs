using CommunityToolkit.Mvvm.ComponentModel;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class ConnectionListItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isPendingDelete;

    public Guid Id { get; init; }

    public Connection Connection { get; init; } = new();

    public string Name { get; init; } = string.Empty;

    public string DatabaseType { get; init; } = string.Empty;

    public string DatabaseTypeColor { get; init; } = "#80B8FF";

    public string DatabaseTypeShort { get; init; } = "DB";

    public string Endpoint { get; init; } = string.Empty;

    public string Database { get; init; } = string.Empty;

    public string Username { get; init; } = string.Empty;

    public static ConnectionListItemViewModel FromConnection(Connection connection)
    {
        var endpoint = connection.Port is null
            ? connection.Host ?? "local"
            : $"{connection.Host ?? "local"}:{connection.Port}";

        var (typeColor, typeShort) = connection.DatabaseType switch
        {
            Domain.Enums.DatabaseType.PostgreSql => ("#4A90D9", "PG"),
            Domain.Enums.DatabaseType.MySql      => ("#E8A838", "MY"),
            Domain.Enums.DatabaseType.MariaDb    => ("#C0392B", "MA"),
            Domain.Enums.DatabaseType.SQLite     => ("#5DA0C5", "SL"),
            Domain.Enums.DatabaseType.SqlServer  => ("#E05C2E", "MS"),
            _                                    => ("#80B8FF", "DB"),
        };

        return new ConnectionListItemViewModel
        {
            Id = connection.Id,
            Connection = connection,
            Name = connection.Name,
            DatabaseType = connection.DatabaseType.ToString(),
            DatabaseTypeColor = typeColor,
            DatabaseTypeShort = typeShort,
            Endpoint = endpoint,
            Database = connection.Database,
            Username = string.IsNullOrWhiteSpace(connection.Username) ? "No username" : connection.Username
        };
    }
}
