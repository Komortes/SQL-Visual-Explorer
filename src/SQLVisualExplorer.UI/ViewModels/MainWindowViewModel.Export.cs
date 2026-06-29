using CommunityToolkit.Mvvm.Input;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand(CanExecute = nameof(CanExportResults))]
    private async Task ExportResultsToCsvAsync()
    {
        if (RequestSaveFilePath is null) return;

        var path = await RequestSaveFilePath("results.csv");
        if (path is null) return;

        try
        {
            var columns = ResultColumns.ToList();
            var lines = new System.Text.StringBuilder();
            lines.AppendLine(string.Join(",", columns.Select(EscapeCsvField)));

            foreach (var row in ResultRows)
            {
                lines.AppendLine(string.Join(",",
                    columns.Select(col =>
                        EscapeCsvField(FormatCellValue(row.Values.GetValueOrDefault(col))))));
            }

            await File.WriteAllTextAsync(path, lines.ToString(), System.Text.Encoding.UTF8);
            QueryStatusMessage = $"Exported {ResultRows.Count} row(s) to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            QueryStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportResults))]
    private async Task ExportResultsToJsonAsync()
    {
        if (RequestSaveFilePath is null) return;

        var path = await RequestSaveFilePath("results.json");
        if (path is null) return;

        try
        {
            var columns = ResultColumns.ToList();
            var rows = ResultRows
                .Select(row => columns.ToDictionary(
                    col => col,
                    col => (object?)FormatCellValue(row.Values.GetValueOrDefault(col))))
                .ToList();

            var json = System.Text.Json.JsonSerializer.Serialize(
                rows,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(path, json, System.Text.Encoding.UTF8);
            QueryStatusMessage = $"Exported {ResultRows.Count} row(s) to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            QueryStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportPlan))]
    private async Task ExportPlanToHtmlAsync()
    {
        if (RequestSaveFilePath is null) return;
        var path = await RequestSaveFilePath("plan.html");
        if (path is null) return;
        try
        {
            var html = PlanHtmlExporter.Generate(SqlText, VisualPlanNodes, PlanIssues, PlanSummaryText);
            await File.WriteAllTextAsync(path, html, System.Text.Encoding.UTF8);
            QueryStatusMessage = $"Plan exported to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            QueryStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private bool CanExportPlan() => VisualPlanNodes.Count > 0;

    [RelayCommand(CanExecute = nameof(CanExportCompare))]
    private async Task ExportCompareToHtmlAsync()
    {
        if (RequestSaveFilePath is null) return;
        var path = await RequestSaveFilePath("compare.html");
        if (path is null) return;
        try
        {
            var html = CompareHtmlExporter.Generate(
                CompareQueryAText, CompareQueryBText,
                CompareResultA, CompareResultB,
                ComparePlanNodesA, ComparePlanNodesB);
            await File.WriteAllTextAsync(path, html, System.Text.Encoding.UTF8);
            CompareStatusMessage = $"Report saved to {Path.GetFileName(path)}.";
        }
        catch (Exception ex)
        {
            CompareStatusMessage = $"Export failed: {ex.Message}";
        }
    }

    private bool CanExportCompare() => CompareHasResults;

    private bool CanExportResults() => ResultRows.Count > 0;

    private static string EscapeCsvField(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private void BuildResultGrid(
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        ResultColumns.Clear();
        ResultColumnsView.Clear();
        ResultRows.Clear();
        _resultRowsBacking.Clear();
        _resultSortColumn = null;
        _resultSortAscending = true;

        if (columns.Count == 0)
        {
            ResultTotalWidth = 0;
            return;
        }

        var widths = columns.ToDictionary(
            col => col,
            col =>
            {
                var maxContent = rows.Count == 0
                    ? 0
                    : rows.Max(row => FormatCellValue(row.GetValueOrDefault(col)).Length);
                var chars = Math.Max(col.Length, maxContent);
                return Math.Clamp(chars * 7.5 + 22, 72, 360);
            });

        foreach (var col in columns)
        {
            ResultColumns.Add(col);
            ResultColumnsView.Add(new ResultColumnViewModel { Name = col, Width = widths[col] });
        }

        ResultTotalWidth = widths.Values.Sum();

        foreach (var row in rows)
        {
            _resultRowsBacking.Add(new QueryResultRowViewModel
            {
                Values = row,
                Cells = columns.Select(col =>
                {
                    var value = row.GetValueOrDefault(col);
                    return new ResultCellViewModel
                    {
                        Text = FormatCellValue(value),
                        Width = widths[col],
                        IsNull = value is null
                    };
                }).ToList()
            });
        }

        foreach (var row in _resultRowsBacking)
            ResultRows.Add(row);
    }

    [RelayCommand]
    private void SortResultsByColumn(string columnName)
    {
        if (string.IsNullOrEmpty(columnName) || _resultRowsBacking.Count == 0)
            return;

        if (_resultSortColumn == columnName)
            _resultSortAscending = !_resultSortAscending;
        else
        {
            _resultSortColumn = columnName;
            _resultSortAscending = true;
        }

        _resultRowsBacking.Sort((a, b) =>
        {
            var left = a.Values.GetValueOrDefault(columnName);
            var right = b.Values.GetValueOrDefault(columnName);
            if (left is null && right is null) return 0;
            if (left is null) return 1;
            if (right is null) return -1;
            var comparison = CompareNonNull(left, right);
            return _resultSortAscending ? comparison : -comparison;
        });

        ResultRows.Clear();
        foreach (var row in _resultRowsBacking)
            ResultRows.Add(row);

        var glyph = _resultSortAscending ? "▲" : "▼";
        var rebuilt = ResultColumnsView
            .Select(col => new ResultColumnViewModel
            {
                Name = col.Name,
                Width = col.Width,
                SortGlyph = col.Name == columnName ? glyph : string.Empty
            })
            .ToList();
        ResultColumnsView.Clear();
        foreach (var col in rebuilt)
            ResultColumnsView.Add(col);
    }

    [RelayCommand]
    private void CopySelectedResultRow(QueryResultRowViewModel? row)
    {
        if (row is null || CopyTextToClipboard is null) return;

        var line = string.Join(
            "\t",
            ResultColumns.Select(col => FormatCellValue(row.Values.GetValueOrDefault(col))));
        _ = CopyTextToClipboard(line);
    }

    private static int CompareNonNull(object left, object right)
    {
        if (left.GetType() == right.GetType() && left is IComparable comparable)
            return comparable.CompareTo(right);

        var leftText = left.ToString() ?? string.Empty;
        var rightText = right.ToString() ?? string.Empty;
        if (double.TryParse(leftText, out var leftNumber) && double.TryParse(rightText, out var rightNumber))
            return leftNumber.CompareTo(rightNumber);

        return string.Compare(leftText, rightText, StringComparison.OrdinalIgnoreCase);
    }
}
