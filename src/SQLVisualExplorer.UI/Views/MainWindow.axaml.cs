using Avalonia.Controls;
using SQLVisualExplorer.UI.ViewModels;

namespace SQLVisualExplorer.UI.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
        : this(new MainWindowViewModel())
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
