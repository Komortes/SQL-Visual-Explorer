using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SQLVisualExplorer.UI.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private ShellNavigationItemViewModel _selectedNavigationItem;

    public MainWindowViewModel()
    {
        NavigationItems =
        [
            new("ED", "Editor", "Query execution workspace"),
            new("PL", "Plan", "Execution plan tree"),
            new("CP", "Compare", "Query comparison"),
            new("HS", "History", "Executed query history"),
            new("DB", "Connect", "Database connections")
        ];

        _selectedNavigationItem = NavigationItems[0];
    }

    public ObservableCollection<ShellNavigationItemViewModel> NavigationItems { get; }

    public string ActiveTitle => SelectedNavigationItem.Label;

    public string ActiveSubtitle => SelectedNavigationItem.Description;

    partial void OnSelectedNavigationItemChanged(ShellNavigationItemViewModel value)
    {
        OnPropertyChanged(nameof(ActiveTitle));
        OnPropertyChanged(nameof(ActiveSubtitle));
    }

    [RelayCommand]
    private void SelectNavigationItem(ShellNavigationItemViewModel navigationItem)
    {
        SelectedNavigationItem = navigationItem;
    }
}
