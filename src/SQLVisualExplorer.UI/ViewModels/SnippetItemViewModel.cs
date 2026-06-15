using CommunityToolkit.Mvvm.ComponentModel;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class SnippetItemViewModel : ObservableObject
{
    public Guid Id { get; }

    public string Name { get; }

    public string? Description { get; }

    public string SqlText { get; }

    public string SqlPreview { get; }

    public string CreatedAt { get; }

    [ObservableProperty]
    private bool _isPendingDelete;

    private SnippetItemViewModel(Snippet snippet)
    {
        Id = snippet.Id;
        Name = snippet.Name;
        Description = snippet.Description;
        SqlText = snippet.SqlText;

        var flat = snippet.SqlText.Replace('\n', ' ').Replace('\r', ' ');
        SqlPreview = flat.Length > 120 ? flat[..120] + "…" : flat;

        CreatedAt = snippet.CreatedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm");
    }

    public static SnippetItemViewModel FromSnippet(Snippet snippet) => new(snippet);
}
