using CommunityToolkit.Mvvm.Input;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel
{
    [RelayCommand]
    private void OpenAdvisorSettings() => IsAdvisorSettingsVisible = true;

    [RelayCommand]
    private void CloseAdvisorSettings() => IsAdvisorSettingsVisible = false;

    [RelayCommand]
    private async Task SaveAdvisorSettingsAsync()
    {
        if (_secretStore is null) return;
        await _secretStore.SaveAsync("advisor-api-key", AdvisorApiKey);
        await _secretStore.SaveAsync("advisor-endpoint", AdvisorEndpoint);
        await _secretStore.SaveAsync("advisor-model", AdvisorModel);
        IsAdvisorSettingsVisible = false;

        if (_advisorService is not null)
            await _advisorService.RefreshAsync();

        RunAdvisorCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task LoadAdvisorSettingsAsync()
    {
        if (_secretStore is null) return;
        AdvisorApiKey   = await _secretStore.LoadAsync("advisor-api-key")  ?? string.Empty;
        AdvisorEndpoint = await _secretStore.LoadAsync("advisor-endpoint") ?? string.Empty;
        AdvisorModel    = await _secretStore.LoadAsync("advisor-model")    ?? string.Empty;

        if (_advisorService is not null)
            await _advisorService.RefreshAsync();

        RunAdvisorCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRunAdvisor))]
    private async Task RunAdvisorAsync()
    {
        if (_advisorService is null) return;
        IsAdvisorRunning = true;
        AdvisorOutput    = string.Empty;
        try
        {
            var dbType = SelectedConnection?.DatabaseType.ToString() ?? "Unknown";
            var result = await _advisorService.AnalyzeAsync(SqlText, dbType, _currentPlanIssues);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(result.Summary);
            if (result.Suggestions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Suggestions:");
                foreach (var s in result.Suggestions)
                    sb.AppendLine($"- {s}");
            }
            if (!string.IsNullOrWhiteSpace(result.RewrittenSql))
            {
                sb.AppendLine();
                sb.AppendLine("Rewritten SQL:");
                sb.AppendLine(result.RewrittenSql);
            }

            AdvisorOutput         = sb.ToString().TrimEnd();
            IsAdvisorPanelVisible = true;
        }
        catch (Exception ex)
        {
            AdvisorOutput = $"Error: {ex.Message}";
        }
        finally
        {
            IsAdvisorRunning = false;
        }
    }

    private bool CanRunAdvisor() =>
        !string.IsNullOrWhiteSpace(SqlText) &&
        !IsAdvisorRunning &&
        (_advisorService?.IsConfigured == true);

    [RelayCommand]
    private void CloseAdvisorPanel() => IsAdvisorPanelVisible = false;
}
