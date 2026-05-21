using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Database.Entities;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class ConnectionService(AppDbContext dbContext) : IConnectionService
{
    public async Task<IReadOnlyList<Connection>> GetConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Connections
            .AsNoTracking()
            .OrderBy(connection => connection.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Connection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Connections
            .AsNoTracking()
            .FirstOrDefaultAsync(connection => connection.Id == id.ToString(), cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Connection> CreateConnectionAsync(CreateConnectionRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new ConnectionEntity
        {
            Id = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        Apply(entity, request);

        dbContext.Connections.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDomain(entity);
    }

    public async Task<Connection?> UpdateConnectionAsync(
        Guid id,
        UpdateConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Connections
            .FirstOrDefaultAsync(connection => connection.Id == id.ToString(), cancellationToken);

        if (entity is null)
        {
            return null;
        }

        Apply(entity, request);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDomain(entity);
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rowsDeleted = await dbContext.Connections
            .Where(connection => connection.Id == id.ToString())
            .ExecuteDeleteAsync(cancellationToken);

        return rowsDeleted > 0;
    }

    private static void Apply(ConnectionEntity entity, CreateConnectionRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.DatabaseType = request.DatabaseType.ToString();
        entity.Host = NormalizeOptionalText(request.Host);
        entity.Port = request.Port;
        entity.Database = request.Database.Trim();
        entity.Username = NormalizeOptionalText(request.Username);
        entity.UseSsl = request.UseSsl;
    }

    private static void Apply(ConnectionEntity entity, UpdateConnectionRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.DatabaseType = request.DatabaseType.ToString();
        entity.Host = NormalizeOptionalText(request.Host);
        entity.Port = request.Port;
        entity.Database = request.Database.Trim();
        entity.Username = NormalizeOptionalText(request.Username);
        entity.UseSsl = request.UseSsl;
    }

    private static Connection ToDomain(ConnectionEntity entity)
    {
        return new Connection
        {
            Id = Guid.Parse(entity.Id),
            Name = entity.Name,
            DatabaseType = Enum.Parse<DatabaseType>(entity.DatabaseType),
            Host = entity.Host,
            Port = entity.Port,
            Database = entity.Database,
            Username = entity.Username,
            UseSsl = entity.UseSsl,
            CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc)),
            LastUsedAt = entity.LastUsed is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(entity.LastUsed.Value, DateTimeKind.Utc))
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
