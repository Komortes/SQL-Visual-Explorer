using SQLVisualExplorer.Application.Services;
using SQLVisualExplorer.Domain.Models;

namespace SQLVisualExplorer.Infrastructure.Tests;

public sealed class GraphLayoutEngineTests
{
    private const double NodeWidth  = GraphLayoutEngine.NodeWidth;
    private const double NodeHeight = GraphLayoutEngine.NodeHeight;
    private const double VGap       = GraphLayoutEngine.VerticalGap;

    [Fact]
    public void SingleNode_IsPositionedAtOrigin()
    {
        var root = Node("root");
        var positions = GraphLayoutEngine.Arrange(root);
        Assert.Equal(0, positions[root.Id].X);
        Assert.Equal(0, positions[root.Id].Y);
    }

    [Fact]
    public void TwoChildren_ArePlacedOnDepth1()
    {
        var left  = Node("left");
        var right = Node("right");
        var root  = Node("root", left, right);

        var positions = GraphLayoutEngine.Arrange(root);

        Assert.Equal(0, positions[root.Id].Y);
        Assert.Equal(NodeHeight + VGap, positions[left.Id].Y);
        Assert.Equal(NodeHeight + VGap, positions[right.Id].Y);
    }

    [Fact]
    public void TwoChildren_LeftIsLeftOfRight()
    {
        var left  = Node("left");
        var right = Node("right");
        var root  = Node("root", left, right);

        var positions = GraphLayoutEngine.Arrange(root);

        Assert.True(positions[left.Id].X < positions[right.Id].X);
    }

    [Fact]
    public void TwoChildren_RootIsCenteredAboveThem()
    {
        var left  = Node("left");
        var right = Node("right");
        var root  = Node("root", left, right);

        var positions = GraphLayoutEngine.Arrange(root);

        // Root center must equal the midpoint of the children's centers.
        var childrenMidpoint = (positions[left.Id].X + positions[right.Id].X) / 2;
        Assert.Equal(childrenMidpoint, positions[root.Id].X, 0.01);
    }

    [Fact]
    public void AsymmetricSubtrees_RootCentersOverChildCentersNotSpan()
    {
        // Left child carries a wide subtree; right child is a leaf. The root
        // must sit over the midpoint of its direct children's centers, not the
        // center of the whole subtree bounding box (which would lean left).
        var grandLeft  = Node("gc1");
        var grandRight = Node("gc2");
        var left  = Node("left", grandLeft, grandRight);
        var right = Node("right");
        var root  = Node("root", left, right);

        var positions = GraphLayoutEngine.Arrange(root);

        var leftCenter  = positions[left.Id].X  + NodeWidth / 2;
        var rightCenter = positions[right.Id].X + NodeWidth / 2;
        var rootCenter  = positions[root.Id].X  + NodeWidth / 2;
        Assert.Equal((leftCenter + rightCenter) / 2, rootCenter, 0.01);
    }

    [Fact]
    public void AllNodes_ArePresent()
    {
        var leaf  = Node("leaf");
        var child = Node("child", leaf);
        var root  = Node("root", child);

        var positions = GraphLayoutEngine.Arrange(root);

        Assert.Equal(3, positions.Count);
    }

    private static PlanNode Node(string label, params PlanNode[] children) =>
        new() { Label = label, Children = children };
}
