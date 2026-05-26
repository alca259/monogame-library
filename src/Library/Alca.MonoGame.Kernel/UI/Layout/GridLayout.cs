namespace Alca.MonoGame.Kernel.UI.Layout;

/// <summary>
/// Arranges children in a configurable grid of rows and columns.
/// Columns and rows support Fixed, Auto, and Star sizing modes.
/// Children are assigned to cells via <see cref="SetCell"/> and can span multiple rows or columns.
/// </summary>
public sealed class GridLayout : UIContainer
{
    // Private record for cell placement data.
    private readonly record struct CellInfo(int Row, int Col, int RowSpan, int ColSpan);

    private readonly List<GridTrack> _columnDefinitions = new(4);
    private readonly List<GridTrack> _rowDefinitions = new(4);
    private readonly Dictionary<UIElement, CellInfo> _cells = new(16);

    // Pre-allocated track-size buffers; grown lazily, never shrunk.
    private float[] _colWidths = Array.Empty<float>();
    private float[] _rowHeights = Array.Empty<float>();
    private int[] _colX = Array.Empty<int>();
    private int[] _rowY = Array.Empty<int>();

    /// <summary>Column definitions; add tracks before arranging children.</summary>
    public IList<GridTrack> ColumnDefinitions => _columnDefinitions;

    /// <summary>Row definitions; add tracks before arranging children.</summary>
    public IList<GridTrack> RowDefinitions => _rowDefinitions;

    /// <summary>Horizontal alignment of children within their cell when smaller than the cell.</summary>
    public HAlign CellHAlign { get; set; } = HAlign.Left;

    /// <summary>Vertical alignment of children within their cell when smaller than the cell.</summary>
    public VAlign CellVAlign { get; set; } = VAlign.Top;

    /// <summary>Assigns <paramref name="child"/> to the specified grid cell with optional span.</summary>
    /// <param name="child">The child element to place.</param>
    /// <param name="row">Zero-based row index.</param>
    /// <param name="col">Zero-based column index.</param>
    /// <param name="rowSpan">Number of rows to span (default 1).</param>
    /// <param name="colSpan">Number of columns to span (default 1).</param>
    public void SetCell(UIElement child, int row, int col, int rowSpan = 1, int colSpan = 1)
    {
        _cells[child] = new CellInfo(row, col, rowSpan, colSpan);
        Invalidate();
    }

    /// <inheritdoc />
    public override void Measure(Vector2 availableSize)
    {
        for (int i = 0; i < Children.Count; i++)
            Children[i].Measure(availableSize);

        DesiredSize = availableSize;
    }

    /// <inheritdoc />
    public override void Arrange(Rectangle finalBounds)
    {
        Bounds = finalBounds;

        int colCount = _columnDefinitions.Count;
        int rowCount = _rowDefinitions.Count;

        if (colCount == 0 || rowCount == 0)
            return;

        // Grow buffers lazily — no allocation on repeated Arrange calls with same grid size.
        if (_colWidths.Length < colCount) _colWidths = new float[colCount];
        if (_rowHeights.Length < rowCount) _rowHeights = new float[rowCount];
        if (_colX.Length < colCount) _colX = new int[colCount];
        if (_rowY.Length < rowCount) _rowY = new int[rowCount];

        // --- Pass 1: Fixed tracks ---
        float usedW = 0f;
        float usedH = 0f;
        float starColWeight = 0f;
        float starRowWeight = 0f;

        for (int c = 0; c < colCount; c++)
        {
            GridTrack t = _columnDefinitions[c];
            if (t.SizeMode == GridSizeMode.Fixed) { _colWidths[c] = t.Value; usedW += t.Value; }
            else if (t.SizeMode == GridSizeMode.Star) { _colWidths[c] = 0f; starColWeight += t.Value; }
            else { _colWidths[c] = 0f; } // Auto — resolved in Pass 2
        }

        for (int r = 0; r < rowCount; r++)
        {
            GridTrack t = _rowDefinitions[r];
            if (t.SizeMode == GridSizeMode.Fixed) { _rowHeights[r] = t.Value; usedH += t.Value; }
            else if (t.SizeMode == GridSizeMode.Star) { _rowHeights[r] = 0f; starRowWeight += t.Value; }
            else { _rowHeights[r] = 0f; } // Auto — resolved in Pass 2
        }

        // --- Pass 2: Auto tracks — size to largest span-1 child in that track ---
        for (int i = 0; i < Children.Count; i++)
        {
            UIElement child = Children[i];
            if (!_cells.TryGetValue(child, out CellInfo info)) continue;

            int col = info.Col;
            int row = info.Row;
            if (col < 0 || col >= colCount || row < 0 || row >= rowCount) continue;

            Vector2 ds = child.DesiredSize;

            if (info.ColSpan == 1 && _columnDefinitions[col].SizeMode == GridSizeMode.Auto)
            {
                if (ds.X > _colWidths[col]) _colWidths[col] = ds.X;
            }

            if (info.RowSpan == 1 && _rowDefinitions[row].SizeMode == GridSizeMode.Auto)
            {
                if (ds.Y > _rowHeights[row]) _rowHeights[row] = ds.Y;
            }
        }

        for (int c = 0; c < colCount; c++)
            if (_columnDefinitions[c].SizeMode == GridSizeMode.Auto) usedW += _colWidths[c];

        for (int r = 0; r < rowCount; r++)
            if (_rowDefinitions[r].SizeMode == GridSizeMode.Auto) usedH += _rowHeights[r];

        // --- Pass 3: Star tracks — distribute remaining space proportionally ---
        float remainW = finalBounds.Width - usedW;
        float remainH = finalBounds.Height - usedH;

        for (int c = 0; c < colCount; c++)
        {
            if (_columnDefinitions[c].SizeMode == GridSizeMode.Star)
                _colWidths[c] = starColWeight > 0f ? remainW * (_columnDefinitions[c].Value / starColWeight) : 0f;
        }

        for (int r = 0; r < rowCount; r++)
        {
            if (_rowDefinitions[r].SizeMode == GridSizeMode.Star)
                _rowHeights[r] = starRowWeight > 0f ? remainH * (_rowDefinitions[r].Value / starRowWeight) : 0f;
        }

        // --- Compute track origins ---
        _colX[0] = finalBounds.X;
        for (int c = 1; c < colCount; c++)
            _colX[c] = _colX[c - 1] + (int)_colWidths[c - 1];

        _rowY[0] = finalBounds.Y;
        for (int r = 1; r < rowCount; r++)
            _rowY[r] = _rowY[r - 1] + (int)_rowHeights[r - 1];

        // --- Arrange children into their cells ---
        for (int i = 0; i < Children.Count; i++)
        {
            UIElement child = Children[i];
            if (!_cells.TryGetValue(child, out CellInfo info)) continue;

            int col = info.Col;
            int row = info.Row;
            if (col < 0 || col >= colCount || row < 0 || row >= rowCount) continue;

            // Accumulate cell rectangle spanning rowSpan/colSpan tracks.
            int cellX = _colX[col];
            int cellY = _rowY[row];
            int cellW = 0;
            int cellH = 0;

            int endCol = Math.Min(col + info.ColSpan, colCount);
            for (int c = col; c < endCol; c++) cellW += (int)_colWidths[c];

            int endRow = Math.Min(row + info.RowSpan, rowCount);
            for (int r = row; r < endRow; r++) cellH += (int)_rowHeights[r];

            // Apply cell alignment — Left/Top stretches the child to fill the cell.
            Vector2 ds = child.DesiredSize;
            int childX, childY, childW, childH;

            switch (CellHAlign)
            {
                case HAlign.Center:
                    childX = cellX + (cellW - (int)ds.X) / 2;
                    childW = (int)ds.X;
                    break;
                case HAlign.Right:
                    childX = cellX + cellW - (int)ds.X;
                    childW = (int)ds.X;
                    break;
                default: // Left = stretch to cell width
                    childX = cellX;
                    childW = cellW;
                    break;
            }

            switch (CellVAlign)
            {
                case VAlign.Middle:
                    childY = cellY + (cellH - (int)ds.Y) / 2;
                    childH = (int)ds.Y;
                    break;
                case VAlign.Bottom:
                    childY = cellY + cellH - (int)ds.Y;
                    childH = (int)ds.Y;
                    break;
                default: // Top = stretch to cell height
                    childY = cellY;
                    childH = cellH;
                    break;
            }

            child.Arrange(new Rectangle(childX, childY, childW, childH));
        }
    }

    /// <inheritdoc />
    protected override void OnChildRemoved(UIElement child)
    {
        _cells.Remove(child);
    }
}
