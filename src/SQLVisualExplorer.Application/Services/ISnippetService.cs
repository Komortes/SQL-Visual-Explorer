using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public interface ISnippetService
{
    Task<IReadOnlyList<Snippet>> GetSnippetsAsync(CancellationToken cancellationToken = default);

    Task<Snippet> CreateSnippetAsync(CreateSnippetRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteSnippetAsync(Guid id, CancellationToken cancellationToken = default);
}
