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

        var centerOfChildren = (positions[left.Id].X + positions[right.Id].X) / 2;
        Assert.Equal(centerOfChildren, positions[root.Id].X + NodeWidth / 2, 0.01);
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
