using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Enums;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Database.Entities;
using SQLVisualExplorer.Infrastructure.Drivers;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class ConnectionService(
    AppDbContext dbContext,
    IEnumerable<IDatabaseDriver> drivers,
    ISecretStore secretStore) : IConnectionService
{
    public async Task<IReadOnlyList<Connection>> GetConnectionsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Connections
            .AsNoTracking()
            .OrderBy(connection => connection.Name)
            .ToListAsync(cancellationToken);

        var results = new List<Connection>(entities.Count);
        foreach (var entity in entities)
        {
            var password = await secretStore.LoadAsync(SecretKey(entity.Id), cancellationToken);
            results.Add(ToDomain(entity, password));
        }
        return results;
    }

    public async Task<Connection?> GetConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Connections
            .AsNoTracking()
            .FirstOrDefaultAsync(connection => connection.Id == id.ToString(), cancellationToken);

        if (entity is null) return null;
        var password = await secretStore.LoadAsync(SecretKey(entity.Id), cancellationToken);
        return ToDomain(entity, password);
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

        var password = NormalizeOptionalText(request.Password);
        if (password is not null)
            await secretStore.SaveAsync(SecretKey(entity.Id), password, cancellationToken);

        return ToDomain(entity, password);
    }

    public async Task<Connection?> UpdateConnectionAsync(
        Guid id,
        UpdateConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Connections
            .FirstOrDefaultAsync(connection => connection.Id == id.ToString(), cancellationToken);

        if (entity is null) return null;

        Apply(entity, request);
        await dbContext.SaveChangesAsync(cancellationToken);

        var password = request.UpdatePassword
            ? NormalizeOptionalText(request.Password)
            : await secretStore.LoadAsync(SecretKey(entity.Id), cancellationToken);

        if (request.UpdatePassword)
        {
            if (password is not null)
                await secretStore.SaveAsync(SecretKey(entity.Id), password, cancellationToken);
            else
                await secretStore.DeleteAsync(SecretKey(entity.Id), cancellationToken);
        }

        return ToDomain(entity, password);
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var idStr = id.ToString();
        var rowsDeleted = await dbContext.Connections
            .Where(connection => connection.Id == idStr)
            .ExecuteDeleteAsync(cancellationToken);

        if (rowsDeleted > 0)
            await secretStore.DeleteAsync(SecretKey(idStr), cancellationToken);

        return rowsDeleted > 0;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(
        CreateConnectionRequest request,
        CancellationToken cancellationToken = default)
    {
        var driver = drivers.FirstOrDefault(candidate => candidate.Supports(request.DatabaseType));

        if (driver is null)
        {
            return ConnectionTestResult.Failure($"{request.DatabaseType} test connection is not supported yet.");
        }

        var connection = new Connection
        {
            Name = request.Name,
            DatabaseType = request.DatabaseType,
            Host = NormalizeOptionalText(request.Host),
            Port = request.Port,
            Database = request.Database.Trim(),
            Username = NormalizeOptionalText(request.Username),
            Password = NormalizeOptionalText(request.Password),
            UseSsl = request.UseSsl
        };

        return await driver.TestConnectionAsync(connection, cancellationToken);
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

    private static Connection ToDomain(ConnectionEntity entity, string? password = null)
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
            Password = password,
            UseSsl = entity.UseSsl,
            CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc)),
            LastUsedAt = entity.LastUsed is null
                ? null
                : new DateTimeOffset(DateTime.SpecifyKind(entity.LastUsed.Value, DateTimeKind.Utc))
        };
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SecretKey(string id) => $"connection/{id}";
}
