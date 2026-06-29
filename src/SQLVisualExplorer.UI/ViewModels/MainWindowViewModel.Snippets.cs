using CommunityToolkit.Mvvm.Input;
using SQLVisualExplorer.Application.Services;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    public async Task LoadSnippetsAsync()
    {
        var snippets = await _snippetService.GetSnippetsAsync();

        _allSnippetItems.Clear();
        _allSnippetItems.AddRange(snippets.Select(SnippetItemViewModel.FromSnippet));
        ApplySnippetFilter();

        SnippetStatusMessage = Snippets.Count == 0
            ? "No saved snippets yet."
            : $"{Snippets.Count} snippet(s).";
    }

    [RelayCommand(CanExecute = nameof(CanSaveSnippet))]
    private async Task SaveSnippetAsync()
    {
        var snippet = await _snippetService.CreateSnippetAsync(new CreateSnippetRequest
        {
            Name = NewSnippetName,
            Description = NewSnippetDescription,
            SqlText = NewSnippetSqlText,
            Tags = ParseSnippetTags(NewSnippetTags)
        });

        _allSnippetItems.Add(SnippetItemViewModel.FromSnippet(snippet));
        ApplySnippetFilter();
        NewSnippetName = string.Empty;
        NewSnippetDescription = string.Empty;
        NewSnippetSqlText = string.Empty;
        NewSnippetTags = string.Empty;
        SnippetStatusMessage = $"Saved snippet \"{snippet.Name}\".";
    }

    private bool CanSaveSnippet() => !string.IsNullOrWhiteSpace(NewSnippetName);

    [RelayCommand]
    private void OpenSnippet(SnippetItemViewModel item)
    {
        SqlText = item.SqlText;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "ED");
    }

    [RelayCommand]
    private void RequestDeleteSnippet(SnippetItemViewModel item) =>
        item.IsPendingDelete = true;

    [RelayCommand]
    private void CancelDeleteSnippet(SnippetItemViewModel item) =>
        item.IsPendingDelete = false;

    [RelayCommand]
    private async Task DeleteSnippetAsync(SnippetItemViewModel item)
    {
        item.IsPendingDelete = false;
        await _snippetService.DeleteSnippetAsync(item.Id);
        _allSnippetItems.Remove(item);
        ApplySnippetFilter();
        SnippetStatusMessage = $"Deleted \"{item.Name}\".";
    }

    [RelayCommand]
    private void SaveCurrentQueryAsSnippet()
    {
        NewSnippetName = string.Empty;
        NewSnippetDescription = string.Empty;
        NewSnippetSqlText = SqlText;
        NewSnippetTags = string.Empty;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "SN");
    }

    [RelayCommand]
    private async Task OpenSnippetsPopupAsync()
    {
        if (_allSnippetItems.Count == 0)
            await LoadSnippetsAsync();
        SnippetsPopupSearchText = string.Empty;
        RefreshPopupSnippets();
        IsSnippetsPopupVisible = true;
    }

    [RelayCommand(CanExecute = nameof(IsSnippetsPopupVisible))]
    private void CloseSnippetsPopup() => IsSnippetsPopupVisible = false;

    [RelayCommand]
    private void OpenSnippetFromPopup(SnippetItemViewModel item)
    {
        SqlText = item.SqlText;
        IsSnippetsPopupVisible = false;
        SelectedNavigationItem = NavigationItems.First(n => n.Code == "ED");
    }

    partial void OnSnippetFilterTextChanged(string value) => ApplySnippetFilter();

    partial void OnSnippetsPopupSearchTextChanged(string value) => RefreshPopupSnippets();

    private void ApplySnippetFilter()
    {
        var term = SnippetFilterText.Trim();
        var filtered = string.IsNullOrWhiteSpace(term)
            ? _allSnippetItems
            : _allSnippetItems.Where(item =>
                item.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (item.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || item.SqlText.Contains(term, StringComparison.OrdinalIgnoreCase)
                || item.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)));

        Snippets.Clear();

        foreach (var item in filtered.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
            Snippets.Add(item);
    }

    private void RefreshPopupSnippets()
    {
        PopupSnippets.Clear();
        var term = SnippetsPopupSearchText.Trim();
        foreach (var snippet in _allSnippetItems)
        {
            if (string.IsNullOrEmpty(term) ||
                snippet.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                snippet.SqlPreview.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (snippet.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                snippet.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                PopupSnippets.Add(snippet);
            }
        }
    }

    private static IReadOnlyList<string> ParseSnippetTags(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(tag => tag.TrimStart('#'))
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
