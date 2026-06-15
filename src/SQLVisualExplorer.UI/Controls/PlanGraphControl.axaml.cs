using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using SQLVisualExplorer.UI.ViewModels;

namespace SQLVisualExplorer.UI.Controls;

public partial class PlanGraphControl : UserControl
{
    private const double NodeWidth  = 180;
    private const double NodeHeight = 64;

    private static readonly IBrush EdgeBrush = new SolidColorBrush(Color.Parse("#2D404E"));

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

    private Point? _panStart;
    private Vector _panBaseOffset;

    public PlanGraphControl()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);
        GraphCanvas.PointerPressed  += OnCanvasPointerPressed;
        GraphCanvas.PointerMoved    += OnCanvasPointerMoved;
        GraphCanvas.PointerReleased += OnCanvasPointerReleased;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        RemoveHandler(PointerWheelChangedEvent, OnPointerWheelChanged);
        GraphCanvas.PointerPressed  -= OnCanvasPointerPressed;
        GraphCanvas.PointerMoved    -= OnCanvasPointerMoved;
        GraphCanvas.PointerReleased -= OnCanvasPointerReleased;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainWindowViewModel vm)
            vm.ComputeFitZoom = ComputeFitZoomForViewport;
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

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        vm.GraphZoom = Math.Clamp(vm.GraphZoom + e.Delta.Y * 0.15, 0.25, 3.0);
        e.Handled = true;
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not (Canvas or Line or Polygon)) return;
        _panStart = e.GetPosition(this);
        _panBaseOffset = ScrollContainer.Offset;
        e.Pointer.Capture(GraphCanvas);
        Cursor = new Cursor(StandardCursorType.SizeAll);
        e.Handled = true;
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_panStart is not { } start) return;
        var delta = e.GetPosition(this) - start;
        ScrollContainer.Offset = new Vector(
            Math.Max(0, _panBaseOffset.X - delta.X),
            Math.Max(0, _panBaseOffset.Y - delta.Y));
        e.Handled = true;
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_panStart is null) return;
        _panStart = null;
        e.Pointer.Capture(null);
        Cursor = Cursor.Default;
    }

    private double ComputeFitZoomForViewport()
    {
        var vpW = ScrollContainer.Bounds.Width;
        var vpH = ScrollContainer.Bounds.Height;
        if (vpW <= 0 || vpH <= 0 || CanvasWidth <= 0 || CanvasHeight <= 0) return 1.0;
        return Math.Min(1.0, Math.Min(vpW / CanvasWidth, vpH / CanvasHeight)) * 0.9;
    }

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
            var arrowTipY  = edge.Y2;
            var arrowBaseY = edge.Y2 - 7;

            var line = new Line
            {
                StartPoint      = new Point(edge.X1, edge.Y1),
                EndPoint        = new Point(edge.X2, arrowBaseY),
                Stroke          = EdgeBrush,
                StrokeThickness = 1.5,
                StrokeLineCap   = PenLineCap.Round,
            };

            var arrow = new Polygon { Fill = EdgeBrush };
            arrow.Points.Add(new Point(edge.X2 - 5, arrowBaseY));
            arrow.Points.Add(new Point(edge.X2 + 5, arrowBaseY));
            arrow.Points.Add(new Point(edge.X2,     arrowTipY));

            GraphCanvas.Children.Add(line);
            GraphCanvas.Children.Add(arrow);
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
            node.IssueText == "OK"       ? "#0F2014" :
            node.IssueText == "Critical" ? "#2A1A1A" : "#1E1A10"));

        var badgeText = node.IssueText switch
        {
            "OK"       => "OK",
            "Critical" => "✕ CRIT",
            _          => "⚠ WARN",
        };

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

        // Proportional fill via star columns so the bar can never exceed the
        // node's inner width, regardless of cost magnitude.
        var ratio = Math.Clamp(node.CostRatio, 0.0, 1.0);

        var barFill = new Border
        {
            CornerRadius = new CornerRadius(2),
            Background   = accentBrush,
        };

        var barFillGrid = new Grid
        {
            Height = 4,
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(new GridLength(ratio, GridUnitType.Star)),
                new ColumnDefinition(new GridLength(Math.Max(0.0001, 1.0 - ratio), GridUnitType.Star)),
            },
        };
        Grid.SetColumn(barFill, 0);
        barFillGrid.Children.Add(barFill);

        var barGrid = new Grid { Children = { barTrack, barFillGrid } };

        var content = new StackPanel
        {
            Spacing  = 6,
            Children = { topRow, secondary, barGrid },
        };

        var border = new Border
        {
            Width           = NodeWidth,
            MinHeight       = NodeHeight,
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
