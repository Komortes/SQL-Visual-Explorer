using CommunityToolkit.Mvvm.Input;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private void SelectConnection(ConnectionListItemViewModel item)
    {
        SelectedConnection = item;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "ED");
    }

    [RelayCommand]
    private void RequestDeleteConnection(ConnectionListItemViewModel item) =>
        item.IsPendingDelete = true;

    [RelayCommand]
    private void CancelDeleteConnection(ConnectionListItemViewModel item) =>
        item.IsPendingDelete = false;

    [RelayCommand]
    private async Task DeleteConnectionAsync(ConnectionListItemViewModel item)
    {
        item.IsPendingDelete = false;
        await _connectionService.DeleteConnectionAsync(item.Id);
        Connections.Remove(item);

        if (SelectedConnection?.Id == item.Id)
            SelectedConnection = Connections.FirstOrDefault();

        ConnectionStatusMessage = $"Deleted \"{item.Name}\".";
    }

    [RelayCommand]
    public async Task LoadConnectionsAsync()
    {
        var connections = await _connectionService.GetConnectionsAsync();

        Connections.Clear();

        foreach (var connection in connections)
            Connections.Add(ConnectionListItemViewModel.FromConnection(connection));

        SelectedConnection ??= Connections.FirstOrDefault();

        ConnectionStatusMessage = Connections.Count == 0
            ? "No saved connections yet."
            : $"{Connections.Count} saved connection(s).";
    }

    [RelayCommand(CanExecute = nameof(CanSaveConnection))]
    private async Task SaveConnectionAsync()
    {
        if (!TryParsePort(out var port))
        {
            ConnectionStatusMessage = "Port must be a number.";
            return;
        }

        try
        {
            var isEditing = IsEditingConnection;
            Connection connection;

            if (EditingConnectionId is { } connectionId)
            {
                var updated = await _connectionService.UpdateConnectionAsync(connectionId, new UpdateConnectionRequest
                {
                    Name = NewConnectionName,
                    DatabaseType = NewConnectionDatabaseType,
                    Host = NewConnectionHost,
                    Port = port,
                    Database = NewConnectionDatabase,
                    Username = NewConnectionUsername,
                    Password = NewConnectionPassword,
                    UpdatePassword = true,
                    UseSsl = NewConnectionUseSsl
                });

                if (updated is null)
                {
                    ConnectionStatusMessage = "Connection no longer exists.";
                    return;
                }

                connection = updated;
                var existingItem = Connections.FirstOrDefault(item => item.Id == connection.Id);
                if (existingItem is not null)
                    Connections[Connections.IndexOf(existingItem)] =
                        ConnectionListItemViewModel.FromConnection(connection);
            }
            else
            {
                connection = await _connectionService.CreateConnectionAsync(new CreateConnectionRequest
                {
                    Name = NewConnectionName,
                    DatabaseType = NewConnectionDatabaseType,
                    Host = NewConnectionHost,
                    Port = port,
                    Database = NewConnectionDatabase,
                    Username = NewConnectionUsername,
                    Password = NewConnectionPassword,
                    UseSsl = NewConnectionUseSsl
                });

                Connections.Add(ConnectionListItemViewModel.FromConnection(connection));
            }

            var connectionItem = Connections.First(item => item.Id == connection.Id);
            SelectedConnection = connectionItem;
            SelectedConnectionPassword = connection.Password ?? string.Empty;
            ClearConnectionForm();

            ConnectionStatusMessage =
                $"{(isEditing ? "Updated" : "Saved")} connection \"{connection.Name}\".";
        }
        catch (Exception exception)
        {
            ConnectionStatusMessage =
                $"Could not save connection: {GetFriendlyErrorMessage(exception)}";
        }
    }

    [RelayCommand]
    private void EditConnection(ConnectionListItemViewModel item)
    {
        var connection = item.Connection;

        EditingConnectionId = connection.Id;
        NewConnectionName = connection.Name;
        NewConnectionDatabaseType = connection.DatabaseType;
        NewConnectionHost = connection.Host ?? "localhost";
        NewConnectionPort = connection.Port?.ToString() ?? string.Empty;
        NewConnectionDatabase = connection.Database;
        NewConnectionUsername = connection.Username ?? string.Empty;
        NewConnectionPassword = connection.Password ?? string.Empty;
        NewConnectionUseSsl = connection.UseSsl;
        ConnectionStatusMessage =
            $"Editing \"{connection.Name}\". Leave password empty to remove it from the keychain.";
    }

    [RelayCommand]
    private void CancelConnectionEdit()
    {
        ClearConnectionForm();
        ConnectionStatusMessage = "Connection edit cancelled.";
    }

    [RelayCommand(CanExecute = nameof(CanSaveConnection))]
    private async Task TestConnectionAsync()
    {
        if (!TryParsePort(out var port))
        {
            ConnectionStatusMessage = "Port must be a number.";
            return;
        }

        ConnectionStatusMessage = "Testing connection...";

        var result = await _connectionService.TestConnectionAsync(new CreateConnectionRequest
        {
            Name = NewConnectionName,
            DatabaseType = NewConnectionDatabaseType,
            Host = NewConnectionHost,
            Port = port,
            Database = NewConnectionDatabase,
            Username = NewConnectionUsername,
            Password = NewConnectionPassword,
            UseSsl = NewConnectionUseSsl
        });

        ConnectionStatusMessage = result.Message;

        if (result.Succeeded)
            SelectedConnectionPassword = NewConnectionPassword;
    }

    private bool CanSaveConnection() =>
        !string.IsNullOrWhiteSpace(NewConnectionName) &&
        !string.IsNullOrWhiteSpace(NewConnectionDatabase);

    partial void OnSelectedConnectionChanged(ConnectionListItemViewModel? value) =>
        SelectedConnectionPassword = value?.Connection?.Password ?? string.Empty;

    private void ClearConnectionForm()
    {
        EditingConnectionId = null;
        NewConnectionName = string.Empty;
        NewConnectionDatabaseType = DatabaseType.PostgreSql;
        NewConnectionHost = "localhost";
        NewConnectionPort = "5432";
        NewConnectionDatabase = string.Empty;
        NewConnectionUsername = string.Empty;
        NewConnectionPassword = string.Empty;
        NewConnectionUseSsl = false;
    }

    private bool TryParsePort(out int? port)
    {
        port = null;

        if (IsLocalDatabaseConnection || string.IsNullOrWhiteSpace(NewConnectionPort))
            return true;

        if (!int.TryParse(NewConnectionPort, out var parsedPort))
            return false;

        port = parsedPort;
        return true;
    }

    private Connection CreateExecutableConnection()
    {
        var connection = SelectedConnection?.Connection
            ?? throw new InvalidOperationException("Select a connection first.");

        return new Connection
        {
            Id = connection.Id,
            Name = connection.Name,
            DatabaseType = connection.DatabaseType,
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.Database,
            Username = connection.Username,
            Password = string.IsNullOrWhiteSpace(SelectedConnectionPassword)
                ? null
                : SelectedConnectionPassword,
            UseSsl = connection.UseSsl,
            CreatedAt = connection.CreatedAt,
            LastUsedAt = connection.LastUsedAt
        };
    }
}
