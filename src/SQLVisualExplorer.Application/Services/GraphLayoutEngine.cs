using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Application.Services;

public static class GraphLayoutEngine
{
    public const double NodeWidth     = 180;
    public const double NodeHeight    = 64;
    public const double HorizontalGap = 20;
    public const double VerticalGap   = 52;

    public static Dictionary<Guid, (double X, double Y)> Arrange(PlanNode root)
    {
        var subtreeWidths = new Dictionary<Guid, double>();
        ComputeSubtreeWidths(root, subtreeWidths);

        var positions = new Dictionary<Guid, (double X, double Y)>();
        AssignPositions(root, 0, 0, positions, subtreeWidths);
        return positions;
    }

    private static double ComputeSubtreeWidths(PlanNode node, Dictionary<Guid, double> widths)
    {
        if (node.Children.Count == 0)
        {
            widths[node.Id] = NodeWidth;
            return NodeWidth;
        }

        double total = 0;

        foreach (var child in node.Children)
        {
            if (total > 0)
                total += HorizontalGap;

            total += ComputeSubtreeWidths(child, widths);
        }

        widths[node.Id] = Math.Max(total, NodeWidth);
        return widths[node.Id];
    }

    private static void AssignPositions(
        PlanNode node,
        double subtreeLeft,
        int depth,
        Dictionary<Guid, (double X, double Y)> positions,
        IReadOnlyDictionary<Guid, double> widths)
    {
        var subtreeWidth = widths[node.Id];
        var nodeLeft = subtreeLeft + (subtreeWidth - NodeWidth) / 2;

        positions[node.Id] = (nodeLeft, depth * (NodeHeight + VerticalGap));

        var childLeft = subtreeLeft;

        foreach (var child in node.Children)
        {
            AssignPositions(child, childLeft, depth + 1, positions, widths);
            childLeft += widths[child.Id] + HorizontalGap;
        }
    }
}
