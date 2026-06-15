using Avalonia.Controls;
using Avalonia.Platform.Storage;
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

        _viewModel.RequestSaveFilePath = async defaultName =>
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export results",
                SuggestedFileName = defaultName,
                FileTypeChoices =
                [
                    new FilePickerFileType("CSV") { Patterns = ["*.csv"] },
                    new FilePickerFileType("All files") { Patterns = ["*"] },
                ]
            });
            return file?.TryGetLocalPath();
        };

        _viewModel.CopyTextToClipboard = async text =>
        {
            if (Clipboard is not null)
                await Clipboard.SetTextAsync(text);
        };

        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        await _viewModel.LoadConnectionsAsync();
    }
}
