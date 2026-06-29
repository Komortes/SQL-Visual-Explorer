using CommunityToolkit.Mvvm.Input;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    public async Task LoadHistoryAsync()
    {
        var history = await _historyService.GetRecentAsync();

        _allHistoryItems.Clear();
        foreach (var entry in history)
            _allHistoryItems.Add(QueryHistoryItemViewModel.FromEntry(entry));

        ApplyHistoryFilters();
    }

    [RelayCommand]
    private void ApplyHistoryFilters()
    {
        var filtered = _allHistoryItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(HistoryFilterText))
        {
            var term = HistoryFilterText.Trim();
            filtered = filtered.Where(item =>
                item.SqlPreview.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                item.ConnectionName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (HistoryShowSlowOnly)
            filtered = filtered.Where(item =>
                item.DurationMs.HasValue && item.DurationMs.Value >= HistorySlowThresholdMs);

        HistoryItems.Clear();
        foreach (var item in filtered)
            HistoryItems.Add(item);
    }

    [RelayCommand]
    private void ClearHistoryFilters()
    {
        HistoryFilterText = string.Empty;
        HistoryShowSlowOnly = false;
        HistorySlowThresholdMs = 500;
        ApplyHistoryFilters();
    }

    [RelayCommand]
    private void OpenHistoryItem(QueryHistoryItemViewModel item)
    {
        SqlText = item.SqlText;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "ED");
    }

    [RelayCommand]
    private void OpenHistoryPlan(QueryHistoryItemViewModel item)
    {
        if (!item.HasSavedPlan || item.DatabaseType is null ||
            string.IsNullOrWhiteSpace(item.ExplainOutput))
        {
            QueryStatusMessage = "This history entry does not have a reusable execution plan.";
            return;
        }

        try
        {
            SqlText = item.SqlText;
            var plan = _planParserService.Parse(item.DatabaseType.Value, item.ExplainOutput);
            var nodeCount = PresentPlan(plan);
            QueryStatusMessage = $"Loaded saved plan with {nodeCount} node(s).";
            SelectedNavigationItem = NavigationItems.First(n => n.Code == "PL");
        }
        catch (Exception exception)
        {
            QueryStatusMessage =
                $"Could not load saved plan: {GetFriendlyErrorMessage(exception)}";
        }
    }

    [RelayCommand]
    private void CopyHistoryItemSql(QueryHistoryItemViewModel item)
    {
        if (CopyTextToClipboard is not null)
            _ = CopyTextToClipboard(item.SqlText);
    }

    [RelayCommand]
    private void RequestDeleteHistoryItem(QueryHistoryItemViewModel item) =>
        item.IsPendingDelete = true;

    [RelayCommand]
    private void CancelDeleteHistoryItem(QueryHistoryItemViewModel item) =>
        item.IsPendingDelete = false;

    [RelayCommand]
    private async Task DeleteHistoryItemAsync(QueryHistoryItemViewModel item)
    {
        item.IsPendingDelete = false;
        await _historyService.DeleteAsync(item.Id);
        HistoryItems.Remove(item);
    }

    [RelayCommand]
    private void AddHistoryItemToSnippets(QueryHistoryItemViewModel item)
    {
        NewSnippetName = string.Empty;
        NewSnippetDescription = string.Empty;
        NewSnippetSqlText = item.SqlText;
        NewSnippetTags = string.Empty;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "SN");
    }
}
