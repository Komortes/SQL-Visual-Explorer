using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
            Tags = JsonSerializer.Serialize(NormalizeTags(request.Tags)),
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
            Tags = ParseTags(entity.Tags),
            CreatedAt = new DateTimeOffset(DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc))
        };
    }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Select(tag => tag.Trim())
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> ParseTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            return [];
        }

        try
        {
            return NormalizeTags(JsonSerializer.Deserialize<string[]>(tags) ?? []);
        }
        catch (JsonException)
        {
            return NormalizeTags(tags.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
