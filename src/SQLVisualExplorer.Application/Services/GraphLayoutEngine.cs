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
        var y = depth * (NodeHeight + VerticalGap);

        // Leaf: sit centered within its own subtree slot.
        if (node.Children.Count == 0)
        {
            var subtreeWidth = widths[node.Id];
            positions[node.Id] = (subtreeLeft + (subtreeWidth - NodeWidth) / 2, y);
            return;
        }

        // Lay out children left-to-right first...
        var childLeft = subtreeLeft;

        foreach (var child in node.Children)
        {
            AssignPositions(child, childLeft, depth + 1, positions, widths);
            childLeft += widths[child.Id] + HorizontalGap;
        }

        // ...then center the parent over the midpoint of its children's node
        // centers (Reingold-Tilford style) so edges stay symmetric and the
        // tree never leans on asymmetric subtrees.
        var firstCenter = positions[node.Children[0].Id].X + NodeWidth / 2;
        var lastCenter  = positions[node.Children[^1].Id].X + NodeWidth / 2;
        positions[node.Id] = ((firstCenter + lastCenter) / 2 - NodeWidth / 2, y);
    }
}
