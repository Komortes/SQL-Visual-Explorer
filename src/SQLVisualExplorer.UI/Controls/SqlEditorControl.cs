using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;

namespace SQLVisualExplorer.UI.Controls;

public sealed class SqlEditorControl : UserControl
{
    public static readonly StyledProperty<string?> SqlTextProperty =
        AvaloniaProperty.Register<SqlEditorControl, string?>(
            nameof(SqlText),
            defaultBindingMode: BindingMode.TwoWay);

    private readonly TextEditor _editor;
    private bool _isUpdatingFromEditor;

    public string? SqlText
    {
        get => GetValue(SqlTextProperty);
        set => SetValue(SqlTextProperty, value);
    }

    public SqlEditorControl()
    {
        _editor = new TextEditor
        {
            FontFamily = new FontFamily("Menlo, Consolas, monospace"),
            FontSize = 13,
            ShowLineNumbers = true,
            Background = new SolidColorBrush(Color.Parse("#0A0F13")),
            Foreground = new SolidColorBrush(Color.Parse("#CFE3F2")),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(10, 12),
        };

        _editor.TextArea.Background = new SolidColorBrush(Color.Parse("#0A0F13"));

        var sqlHighlighting = HighlightingManager.Instance.GetDefinition("SQL");
        if (sqlHighlighting is not null)
            _editor.SyntaxHighlighting = sqlHighlighting;

        _editor.TextChanged += OnEditorTextChanged;

        Content = _editor;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SqlTextProperty && !_isUpdatingFromEditor)
        {
            var newText = change.GetNewValue<string?>() ?? string.Empty;
            if (_editor.Text != newText)
                _editor.Text = newText;
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        _isUpdatingFromEditor = true;
        try
        {
            SetValue(SqlTextProperty, _editor.Text);
        }
        finally
        {
            _isUpdatingFromEditor = false;
        }
    }
}
