using Microsoft.EntityFrameworkCore;
using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Models;
using SQLVisualExplorer.Infrastructure.Database;
using SQLVisualExplorer.Infrastructure.Database.Entities;

namespace SQLVisualExplorer.Infrastructure.Services;

public sealed class SnippetService(AppDbContext dbContext) : ISnippetService
{
    public async Task<IReadOnlyList<Snippet>> GetSnippetsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Snippets
            .AsNoTracking()
            .OrderBy(snippet => snippet.Name)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Snippet> CreateSnippetAsync(CreateSnippetRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new SnippetEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            SqlText = request.SqlText,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Snippets.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDomain(entity);
    }

    public async Task<bool> DeleteSnippetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rowsDeleted = await dbContext.Snippets
            .Where(snippet => snippet.Id == id.ToString())
            .ExecuteDeleteAsync(cancellationToken);

        return rowsDeleted > 0;
    }

    private static Snippet ToDomain(SnippetEntity entity)
    {
        return new Snippet
        {
            Id = Guid.Parse(entity.Id),
            Name = entity.Name,
            Description = entity.Description,
            SqlText = entity.SqlText,
            Tags = entity.Tags,
            CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc))
        };
    }
}
