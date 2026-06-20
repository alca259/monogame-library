using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Layout;

namespace Alca.MonoGame.Kernel.UnitTests.UI.Layout;

public sealed class GridLayoutTests
{
    #region Helpers

    private sealed class StubElement : UIElement
    {
        private readonly Vector2 _fixedSize;

        public StubElement(float w, float h) => _fixedSize = new Vector2(w, h);

        public override void Measure(Vector2 availableSize) => DesiredSize = _fixedSize;
        public override void Draw(SpriteBatch spriteBatch) { }
    }

    #endregion

    #region Fixed tracks

    [Fact]
    public void Arrange_TwoFixedColumns_ChildrenPlacedAtCorrectX()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(80f));
        grid.ColumnDefinitions.Add(GridTrack.Fixed(120f));
        grid.RowDefinitions.Add(GridTrack.Fixed(50f));

        var a = new StubElement(50f, 30f);
        var b = new StubElement(60f, 30f);
        grid.Add(a); grid.SetCell(a, 0, 0);
        grid.Add(b); grid.SetCell(b, 0, 1);

        grid.Measure(new Vector2(300f, 100f));
        grid.Arrange(new Rectangle(0, 0, 300, 100));

        Assert.Equal(0, a.Bounds.X);
        Assert.Equal(80, b.Bounds.X);
    }

    [Fact]
    public void Arrange_TwoFixedRows_ChildrenPlacedAtCorrectY()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(100f));
        grid.RowDefinitions.Add(GridTrack.Fixed(40f));
        grid.RowDefinitions.Add(GridTrack.Fixed(60f));

        var a = new StubElement(50f, 20f);
        var b = new StubElement(50f, 20f);
        grid.Add(a); grid.SetCell(a, 0, 0);
        grid.Add(b); grid.SetCell(b, 1, 0);

        grid.Measure(new Vector2(200f, 200f));
        grid.Arrange(new Rectangle(0, 0, 200, 200));

        Assert.Equal(0, a.Bounds.Y);
        Assert.Equal(40, b.Bounds.Y);
    }

    #endregion

    #region Star tracks

    [Fact]
    public void Arrange_TwoEqualStarColumns_SplitsWidthEvenly()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Star(1f));
        grid.ColumnDefinitions.Add(GridTrack.Star(1f));
        grid.RowDefinitions.Add(GridTrack.Fixed(50f));

        var a = new StubElement(10f, 10f);
        var b = new StubElement(10f, 10f);
        grid.Add(a); grid.SetCell(a, 0, 0);
        grid.Add(b); grid.SetCell(b, 0, 1);

        grid.Measure(new Vector2(200f, 100f));
        grid.Arrange(new Rectangle(0, 0, 200, 100));

        Assert.Equal(0, a.Bounds.X);
        Assert.Equal(100, b.Bounds.X);
        Assert.Equal(100, a.Bounds.Width);
        Assert.Equal(100, b.Bounds.Width);
    }

    [Fact]
    public void Arrange_StarColumnsWithFixedColumn_StarTakesRemaining()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(60f));
        grid.ColumnDefinitions.Add(GridTrack.Star(1f));
        grid.RowDefinitions.Add(GridTrack.Fixed(50f));

        var a = new StubElement(10f, 10f);
        var b = new StubElement(10f, 10f);
        grid.Add(a); grid.SetCell(a, 0, 0);
        grid.Add(b); grid.SetCell(b, 0, 1);

        grid.Measure(new Vector2(200f, 100f));
        grid.Arrange(new Rectangle(0, 0, 200, 100));

        Assert.Equal(0, a.Bounds.X);
        Assert.Equal(60, b.Bounds.X);
        Assert.Equal(140, b.Bounds.Width); // 200 - 60
    }

    #endregion

    #region Auto tracks

    [Fact]
    public void Arrange_AutoColumn_SizesToLargestChildDesiredWidth()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Auto());
        grid.ColumnDefinitions.Add(GridTrack.Fixed(50f));
        grid.RowDefinitions.Add(GridTrack.Fixed(30f));
        grid.RowDefinitions.Add(GridTrack.Fixed(30f));

        var big = new StubElement(90f, 20f);
        var small = new StubElement(40f, 20f);
        var other = new StubElement(10f, 20f);
        grid.Add(big);   grid.SetCell(big, 0, 0);
        grid.Add(small); grid.SetCell(small, 1, 0);
        grid.Add(other); grid.SetCell(other, 0, 1);

        grid.Measure(new Vector2(200f, 200f));
        grid.Arrange(new Rectangle(0, 0, 200, 200));

        // Auto column should be 90 (max of 90 and 40).
        Assert.Equal(90, other.Bounds.X); // second column starts after 90px auto col
    }

    #endregion

    #region Column span

    [Fact]
    public void Arrange_ColSpan2_ChildFillsBothColumns()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(60f));
        grid.ColumnDefinitions.Add(GridTrack.Fixed(80f));
        grid.RowDefinitions.Add(GridTrack.Fixed(50f));

        var spanning = new StubElement(10f, 10f);
        grid.Add(spanning); grid.SetCell(spanning, 0, 0, colSpan: 2);

        grid.Measure(new Vector2(300f, 100f));
        grid.Arrange(new Rectangle(0, 0, 300, 100));

        Assert.Equal(0, spanning.Bounds.X);
        Assert.Equal(140, spanning.Bounds.Width); // 60 + 80
    }

    #endregion

    #region Row span

    [Fact]
    public void Arrange_RowSpan2_ChildFillsBothRows()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(100f));
        grid.RowDefinitions.Add(GridTrack.Fixed(30f));
        grid.RowDefinitions.Add(GridTrack.Fixed(50f));

        var spanning = new StubElement(10f, 10f);
        grid.Add(spanning); grid.SetCell(spanning, 0, 0, rowSpan: 2);

        grid.Measure(new Vector2(200f, 200f));
        grid.Arrange(new Rectangle(0, 0, 200, 200));

        Assert.Equal(0, spanning.Bounds.Y);
        Assert.Equal(80, spanning.Bounds.Height); // 30 + 50
    }

    #endregion

    #region Cell alignment

    [Fact]
    public void Arrange_HAlignCenter_ChildCenteredInCell()
    {
        var grid = new GridLayout { CellHAlign = HAlign.Center, CellVAlign = VAlign.Top };
        grid.ColumnDefinitions.Add(GridTrack.Fixed(100f));
        grid.RowDefinitions.Add(GridTrack.Fixed(50f));

        var child = new StubElement(40f, 20f);
        grid.Add(child); grid.SetCell(child, 0, 0);

        grid.Measure(new Vector2(200f, 200f));
        grid.Arrange(new Rectangle(0, 0, 200, 200));

        // Centered horizontally: (100 - 40) / 2 = 30
        Assert.Equal(30, child.Bounds.X);
    }

    [Fact]
    public void Arrange_VAlignMiddle_ChildCenteredVerticallyInCell()
    {
        var grid = new GridLayout { CellHAlign = HAlign.Left, CellVAlign = VAlign.Middle };
        grid.ColumnDefinitions.Add(GridTrack.Fixed(100f));
        grid.RowDefinitions.Add(GridTrack.Fixed(60f));

        var child = new StubElement(50f, 20f);
        grid.Add(child); grid.SetCell(child, 0, 0);

        grid.Measure(new Vector2(200f, 200f));
        grid.Arrange(new Rectangle(0, 0, 200, 200));

        // Centered vertically: (60 - 20) / 2 = 20
        Assert.Equal(20, child.Bounds.Y);
    }

    [Fact]
    public void Arrange_HAlignRight_ChildAlignedToRightOfCell()
    {
        var grid = new GridLayout { CellHAlign = HAlign.Right, CellVAlign = VAlign.Top };
        grid.ColumnDefinitions.Add(GridTrack.Fixed(100f));
        grid.RowDefinitions.Add(GridTrack.Fixed(50f));

        var child = new StubElement(40f, 20f);
        grid.Add(child); grid.SetCell(child, 0, 0);

        grid.Measure(new Vector2(200f, 200f));
        grid.Arrange(new Rectangle(0, 0, 200, 200));

        // Right-aligned: 100 - 40 = 60
        Assert.Equal(60, child.Bounds.X);
    }

    [Fact]
    public void Arrange_VAlignBottom_ChildAlignedToBottomOfCell()
    {
        var grid = new GridLayout { CellHAlign = HAlign.Left, CellVAlign = VAlign.Bottom };
        grid.ColumnDefinitions.Add(GridTrack.Fixed(100f));
        grid.RowDefinitions.Add(GridTrack.Fixed(60f));

        var child = new StubElement(50f, 20f);
        grid.Add(child); grid.SetCell(child, 0, 0);

        grid.Measure(new Vector2(200f, 200f));
        grid.Arrange(new Rectangle(0, 0, 200, 200));

        // Bottom-aligned: 60 - 20 = 40
        Assert.Equal(40, child.Bounds.Y);
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Arrange_NoDefinitions_DoesNotThrow()
    {
        var grid = new GridLayout();
        var child = new StubElement(20f, 20f);
        grid.Add(child);
        grid.Measure(new Vector2(100f, 100f));
        grid.Arrange(new Rectangle(0, 0, 100, 100));
        // Child was not placed (no tracks defined) — Bounds remains default.
        Assert.Equal(Rectangle.Empty, child.Bounds);
    }

    [Fact]
    public void Arrange_WithOffset_TrackOriginsShiftedByParentOrigin()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(50f));
        grid.ColumnDefinitions.Add(GridTrack.Fixed(50f));
        grid.RowDefinitions.Add(GridTrack.Fixed(30f));

        var a = new StubElement(20f, 10f);
        var b = new StubElement(20f, 10f);
        grid.Add(a); grid.SetCell(a, 0, 0);
        grid.Add(b); grid.SetCell(b, 0, 1);

        grid.Measure(new Vector2(200f, 100f));
        grid.Arrange(new Rectangle(20, 10, 200, 100));

        Assert.Equal(20, a.Bounds.X);
        Assert.Equal(70, b.Bounds.X); // 20 + 50
        Assert.Equal(10, a.Bounds.Y);
    }

    [Fact]
    public void Remove_CleansUpCellEntry_NoExceptionOnRearrange()
    {
        var grid = new GridLayout();
        grid.ColumnDefinitions.Add(GridTrack.Fixed(100f));
        grid.RowDefinitions.Add(GridTrack.Fixed(50f));

        var child = new StubElement(20f, 20f);
        grid.Add(child);
        grid.SetCell(child, 0, 0);
        grid.Remove(child);

        grid.Measure(new Vector2(200f, 200f));
        grid.Arrange(new Rectangle(0, 0, 200, 200));
        Assert.Equal(Rectangle.Empty, child.Bounds);
    }

    #endregion
}
