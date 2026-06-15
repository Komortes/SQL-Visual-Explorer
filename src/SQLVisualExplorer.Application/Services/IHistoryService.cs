using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public interface IHistoryService
{
    Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default);

    Task<QueryHistoryEntry> RecordAsync(
        RecordQueryHistoryRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
