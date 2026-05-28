using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SQLVisualExplorer.UI.ViewModels;

namespace SQLVisualExplorer.UI.Controls;

public partial class PlanGraphControl : UserControl
{
    private const double NodeWidth  = 180;
    private const double NodeHeight = 64;

    public static readonly StyledProperty<ObservableCollection<PlanNodeVisualItemViewModel>?> NodesSourceProperty =
        AvaloniaProperty.Register<PlanGraphControl, ObservableCollection<PlanNodeVisualItemViewModel>?>(nameof(NodesSource));

    public static readonly StyledProperty<ObservableCollection<GraphEdgeViewModel>?> EdgesSourceProperty =
        AvaloniaProperty.Register<PlanGraphControl, ObservableCollection<GraphEdgeViewModel>?>(nameof(EdgesSource));

    public static readonly StyledProperty<double> CanvasWidthProperty =
        AvaloniaProperty.Register<PlanGraphControl, double>(nameof(CanvasWidth), 800);

    public static readonly StyledProperty<double> CanvasHeightProperty =
        AvaloniaProperty.Register<PlanGraphControl, double>(nameof(CanvasHeight), 600);

    public static readonly StyledProperty<double> GraphZoomProperty =
        AvaloniaProperty.Register<PlanGraphControl, double>(nameof(GraphZoom), 1.0);

    public ObservableCollection<PlanNodeVisualItemViewModel>? NodesSource
    {
        get => GetValue(NodesSourceProperty);
        set => SetValue(NodesSourceProperty, value);
    }

    public ObservableCollection<GraphEdgeViewModel>? EdgesSource
    {
        get => GetValue(EdgesSourceProperty);
        set => SetValue(EdgesSourceProperty, value);
    }

    public double CanvasWidth
    {
        get => GetValue(CanvasWidthProperty);
        set => SetValue(CanvasWidthProperty, value);
    }

    public double CanvasHeight
    {
        get => GetValue(CanvasHeightProperty);
        set => SetValue(CanvasHeightProperty, value);
    }

    public double GraphZoom
    {
        get => GetValue(GraphZoomProperty);
        set => SetValue(GraphZoomProperty, value);
    }

    public PlanGraphControl()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == NodesSourceProperty)
        {
            if (change.OldValue is ObservableCollection<PlanNodeVisualItemViewModel> oldNodes)
                oldNodes.CollectionChanged -= OnNodesChanged;

            if (change.NewValue is ObservableCollection<PlanNodeVisualItemViewModel> newNodes)
                newNodes.CollectionChanged += OnNodesChanged;

            Rebuild();
        }
        else if (change.Property == EdgesSourceProperty)
        {
            if (change.OldValue is ObservableCollection<GraphEdgeViewModel> oldEdges)
                oldEdges.CollectionChanged -= OnEdgesChanged;

            if (change.NewValue is ObservableCollection<GraphEdgeViewModel> newEdges)
                newEdges.CollectionChanged += OnEdgesChanged;

            Rebuild();
        }
        else if (change.Property == CanvasWidthProperty || change.Property == CanvasHeightProperty)
        {
            GraphCanvas.Width  = CanvasWidth;
            GraphCanvas.Height = CanvasHeight;
        }
    }

    private void OnNodesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();
    private void OnEdgesChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        GraphCanvas.Children.Clear();
        GraphCanvas.Width  = CanvasWidth;
        GraphCanvas.Height = CanvasHeight;

        DrawEdges();
        DrawNodes();
    }

    private void DrawEdges()
    {
        if (EdgesSource is null) return;

        foreach (var edge in EdgesSource)
        {
            var line = new Line
            {
                StartPoint      = new Point(edge.X1, edge.Y1),
                EndPoint        = new Point(edge.X2, edge.Y2),
                Stroke          = new SolidColorBrush(Color.Parse("#3A4E5C")),
                StrokeThickness = 1.5,
                StrokeLineCap   = PenLineCap.Round,
            };

            GraphCanvas.Children.Add(line);
        }
    }

    private void DrawNodes()
    {
        if (NodesSource is null) return;

        foreach (var node in NodesSource)
        {
            var card = BuildNodeCard(node);
            Canvas.SetLeft(card, node.GraphX);
            Canvas.SetTop(card,  node.GraphY);
            GraphCanvas.Children.Add(card);
        }
    }

    private Border BuildNodeCard(PlanNodeVisualItemViewModel node)
    {
        var accentBrush = new SolidColorBrush(Color.Parse(node.AccentColor));
        var issueBrush  = new SolidColorBrush(Color.Parse(node.IssueColor));

        var dot = new Ellipse
        {
            Width  = 8,
            Height = 8,
            Fill   = accentBrush,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var label = new TextBlock
        {
            Text         = node.Label,
            FontSize     = 11,
            FontWeight   = FontWeight.SemiBold,
            Foreground   = new SolidColorBrush(Color.Parse("#E7ECEF")),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth     = NodeWidth - 60,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var badgeBg = new SolidColorBrush(Color.Parse(
            node.IssueText == "OK" ? "#0F2014" :
            node.IssueText == "Critical" ? "#2A1A1A" : "#1E1A10"));

        var badgeText = node.IssueText == "OK" ? "OK" :
                        node.IssueText == "Critical" ? "✕ CRIT" : "⚠ WARN";

        var badge = new Border
        {
            Padding           = new Thickness(5, 2),
            CornerRadius      = new CornerRadius(3),
            Background        = badgeBg,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text       = badgeText,
                FontSize   = 9,
                FontWeight = FontWeight.Bold,
                Foreground = issueBrush,
            }
        };

        var topRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing     = 6,
            Children    = { dot, label, badge },
        };

        var secondary = new TextBlock
        {
            Text         = node.CostText + " · " + node.RowsText,
            FontSize     = 10,
            Foreground   = new SolidColorBrush(Color.Parse("#91A0AD")),
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var barTrack = new Border
        {
            Height       = 4,
            CornerRadius = new CornerRadius(2),
            Background   = new SolidColorBrush(Color.Parse("#202B33")),
        };

        var barFill = new Border
        {
            Width               = node.CostBarWidth,
            Height              = 4,
            CornerRadius        = new CornerRadius(2),
            Background          = accentBrush,
            HorizontalAlignment = HorizontalAlignment.Left,
        };

        var barGrid = new Grid { Children = { barTrack, barFill } };

        var content = new StackPanel
        {
            Spacing  = 6,
            Children = { topRow, secondary, barGrid },
        };

        var border = new Border
        {
            Width           = NodeWidth,
            Height          = NodeHeight,
            Padding         = new Thickness(10),
            CornerRadius    = new CornerRadius(8),
            Background      = new SolidColorBrush(Color.Parse("#17212A")),
            BorderThickness = new Thickness(1.5),
            BorderBrush     = new SolidColorBrush(Color.Parse(node.IsSelected ? "#80B8FF" : "#26313A")),
            Child           = content,
            Cursor          = new Cursor(StandardCursorType.Hand),
        };

        border.PointerPressed += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                vm.SelectPlanNodeCommand.Execute(node);
        };

        node.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PlanNodeVisualItemViewModel.IsSelected))
            {
                border.BorderBrush = new SolidColorBrush(
                    Color.Parse(node.IsSelected ? "#80B8FF" : "#26313A"));
            }
        };

        return border;
    }
}
