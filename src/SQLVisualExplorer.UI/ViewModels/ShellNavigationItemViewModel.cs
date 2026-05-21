namespace SQLVisualExplorer.UI.ViewModels;

public sealed class ShellNavigationItemViewModel(string code, string label, string description)
{
    public string Code { get; } = code;

    public string Label { get; } = label;

    public string Description { get; } = description;
}
