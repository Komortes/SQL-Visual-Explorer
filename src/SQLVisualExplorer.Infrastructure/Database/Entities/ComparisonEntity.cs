namespace SQLVisualExplorer.Infrastructure.Database.Entities;

public sealed class ComparisonEntity
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string QueryA { get; set; } = string.Empty;

    public string QueryB { get; set; } = string.Empty;

    public string? ResultJson { get; set; }

    public DateTime CreatedAt { get; set; }
}
