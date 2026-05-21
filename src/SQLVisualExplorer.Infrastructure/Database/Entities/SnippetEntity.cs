namespace SQLVisualExplorer.Infrastructure.Database.Entities;

public sealed class SnippetEntity
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SqlText { get; set; } = string.Empty;

    public string? Tags { get; set; }

    public DateTime CreatedAt { get; set; }
}
