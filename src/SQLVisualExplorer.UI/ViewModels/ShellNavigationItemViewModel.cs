using CommunityToolkit.Mvvm.ComponentModel;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class ShellNavigationItemViewModel(
    string code,
    string label,
    string description,
    string iconPath) : ObservableObject
{
    public string Code { get; } = code;
    public string Label { get; } = label;
    public string Description { get; } = description;
    public string IconPath { get; } = iconPath;

    [ObservableProperty]
    private bool _isActive;

    public string NavBackground => IsActive ? "#1B242C" : "Transparent";
    public double NavOpacity => IsActive ? 1.0 : 0.42;

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(NavBackground));
        OnPropertyChanged(nameof(NavOpacity));
    }
}
