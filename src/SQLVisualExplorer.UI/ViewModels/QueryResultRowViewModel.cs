using Avalonia.Media;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed class QueryResultRowViewModel
{
    public IReadOnlyDictionary<string, object?> Values { get; init; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<ResultCellViewModel> Cells { get; init; } = [];
}

public sealed class ResultCellViewModel
{
    public string Text { get; init; } = string.Empty;

    public double Width { get; init; }

    public bool IsNull { get; init; }

    public IBrush Foreground => IsNull
        ? new SolidColorBrush(Color.Parse("#4A5A66"))
        : new SolidColorBrush(Color.Parse("#CFE3F2"));

    public FontStyle FontStyle => IsNull ? FontStyle.Italic : FontStyle.Normal;
}

public sealed class ResultColumnViewModel
{
    public string Name { get; init; } = string.Empty;

    public double Width { get; init; }

    public string SortGlyph { get; init; } = string.Empty;

    public string Header => string.IsNullOrEmpty(SortGlyph) ? Name : $"{Name} {SortGlyph}";
}
