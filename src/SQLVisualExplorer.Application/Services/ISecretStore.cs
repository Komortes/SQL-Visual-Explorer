namespace SQLVisualExplorer.Application.Services;

public interface ISecretStore
{
    Task SaveAsync(string key, string secret, CancellationToken cancellationToken = default);
    Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
