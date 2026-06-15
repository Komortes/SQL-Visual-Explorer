using CommunityToolkit.Mvvm.ComponentModel;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class ComparePlanResultViewModel : ObservableObject
{
    public string Label         { get; init; } = string.Empty;
    public string RootLabel     { get; init; } = string.Empty;
    public string CostText      { get; init; } = string.Empty;
    public string TimeText      { get; init; } = string.Empty;
    public string EstRowsText   { get; init; } = string.Empty;
    public string ActRowsText   { get; init; } = string.Empty;
    public string NodeCountText { get; init; } = string.Empty;
    public string IssueText     { get; init; } = string.Empty;
    public string IssueColor    { get; init; } = "#7BD88F";

    [ObservableProperty]
    private bool _isWinner;

    public string WinnerBorderBrush => IsWinner ? "#80B8FF" : "#26313A";

    partial void OnIsWinnerChanged(bool value) =>
        OnPropertyChanged(nameof(WinnerBorderBrush));
}
