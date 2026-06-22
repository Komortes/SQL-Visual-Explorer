namespace SQLVisualExplorer.Application.Services;

public sealed class CreateSnippetRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string SqlText { get; set; } = string.Empty;

    public IReadOnlyList<string> Tags { get; set; } = [];
}
