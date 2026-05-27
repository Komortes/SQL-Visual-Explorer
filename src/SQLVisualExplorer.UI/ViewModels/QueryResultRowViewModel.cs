namespace SQLVisualExplorer.UI.ViewModels;

public sealed class QueryResultRowViewModel
{
    public IReadOnlyDictionary<string, object?> Values { get; init; } =
        new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public string DisplayText { get; init; } = string.Empty;

    public static QueryResultRowViewModel FromValues(
        IReadOnlyDictionary<string, object?> values,
        IReadOnlyList<string> columns)
    {
        return new QueryResultRowViewModel
        {
            Values = values,
            DisplayText = string.Join("    ", columns.Select(column => FormatValue(values.GetValueOrDefault(column))))
        };
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            DateTime dateTime => dateTime.ToString("u"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("u"),
            _ => value.ToString() ?? string.Empty
        };
    }
}
