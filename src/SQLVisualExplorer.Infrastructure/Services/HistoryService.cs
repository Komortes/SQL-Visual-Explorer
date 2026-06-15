using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Database.Entities;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class HistoryService(AppDbContext dbContext) : IHistoryService
{
    public async Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);

        var entities = await dbContext.QueryHistory
            .AsNoTracking()
            .Include(history => history.Connection)
            .OrderByDescending(history => history.ExecutedAt)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<QueryHistoryEntry> RecordAsync(
        RecordQueryHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = new QueryHistoryEntity
        {
            Id = Guid.NewGuid().ToString(),
            ConnectionId = request.ConnectionId?.ToString(),
            SqlText = request.SqlText,
            ExecutedAt = DateTime.UtcNow,
            DurationMs = request.Duration is null ? null : (long)Math.Round(request.Duration.Value.TotalMilliseconds),
            RowCount = request.RowCount,
            Status = request.Succeeded ? "success" : "error",
            ErrorMessage = request.ErrorMessage,
            ExplainJson = request.ExplainJson
        };

        dbContext.QueryHistory.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (entity.ConnectionId is not null)
        {
            await dbContext.Entry(entity)
                .Reference(history => history.Connection)
                .LoadAsync(cancellationToken);
        }

        return ToDomain(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var idString = id.ToString();
        var entity = await dbContext.QueryHistory
            .FirstOrDefaultAsync(history => history.Id == idString, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        dbContext.QueryHistory.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static QueryHistoryEntry ToDomain(QueryHistoryEntity entity)
    {
        return new QueryHistoryEntry
        {
            Id = Guid.Parse(entity.Id),
            ConnectionId = entity.ConnectionId is null ? null : Guid.Parse(entity.ConnectionId),
            ConnectionName = entity.Connection?.Name ?? "Unknown connection",
            SqlText = entity.SqlText,
            ExecutedAt = new DateTimeOffset(DateTime.SpecifyKind(entity.ExecutedAt, DateTimeKind.Utc)),
            Duration = entity.DurationMs is null ? null : TimeSpan.FromMilliseconds(entity.DurationMs.Value),
            RowCount = entity.RowCount,
            Status = entity.Status,
            ErrorMessage = entity.ErrorMessage,
            ExplainJson = entity.ExplainJson
        };
    }
}
