using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public interface IConnectionService
{
    Task<IReadOnlyList<Connection>> GetConnectionsAsync(CancellationToken cancellationToken = default);

    Task<Connection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Connection> CreateConnectionAsync(CreateConnectionRequest request, CancellationToken cancellationToken = default);

    Task<Connection?> UpdateConnectionAsync(Guid id, UpdateConnectionRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ConnectionTestResult> TestConnectionAsync(
        CreateConnectionRequest request,
        CancellationToken cancellationToken = default);
}
