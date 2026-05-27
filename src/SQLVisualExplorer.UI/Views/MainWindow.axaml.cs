using Avalonia.Controls;
using SQLVisualExplorer.UI.ViewModels;

namespace SQLVisualExplorer.UI.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
        : this(new MainWindowViewModel())
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;

        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await _viewModel.LoadConnectionsAsync();
    }
}
