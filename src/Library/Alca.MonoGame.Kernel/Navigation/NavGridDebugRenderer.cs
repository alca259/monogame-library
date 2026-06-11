namespace Alca.MonoGame.Kernel.Navigation;

/// <summary>
/// Renders a <see cref="NavGrid"/> as a debug overlay using a <see cref="SpriteBatch"/>.
/// Intended for development and debugging only. Do not use in production builds.
/// No heap allocations occur inside <see cref="Draw"/>.
/// </summary>
public sealed class NavGridDebugRenderer
{
    private readonly Texture2D _pixel;

    /// <summary>Gets or sets the color used for walkable cells. Default is semi-transparent green.</summary>
    public Color WalkableColor { get; set; } = Color.Green * 0.3f;

    /// <summary>Gets or sets the color used for blocked cells. Default is semi-transparent red.</summary>
    public Color BlockedColor { get; set; } = Color.Red * 0.5f;

    /// <summary>Gets or sets the color used for cells with an obstacle. Default is semi-transparent orange.</summary>
    public Color ObstacleColor { get; set; } = Color.Orange * 0.5f;

    /// <summary>Gets or sets the color used for path waypoints. Default is yellow.</summary>
    public Color PathColor { get; set; } = Color.Yellow;

    /// <summary>Gets or sets a value indicating whether the grid cells are drawn. Default is true.</summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the active path waypoints are drawn. Default is true.</summary>
    public bool ShowPath { get; set; } = true;

    /// <summary>
    /// Creates a new <see cref="NavGridDebugRenderer"/>.
    /// </summary>
    /// <param name="pixelTexture">A 1×1 white <see cref="Texture2D"/> used to draw filled rectangles.</param>
    public NavGridDebugRenderer(Texture2D pixelTexture)
    {
        _pixel = pixelTexture;
    }

    /// <summary>
    /// Draws the grid and optionally the active path. Must be called between
    /// <see cref="SpriteBatch.Begin"/> and <see cref="SpriteBatch.End"/>.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, NavGrid grid, NavPath? activePath = null)
    {
        if (ShowGrid)
            DrawGrid(spriteBatch, grid);

        if (ShowPath && activePath is not null && !activePath.IsEmpty)
            DrawPath(spriteBatch, grid, activePath);
    }

    #region Internal
    private void DrawGrid(SpriteBatch spriteBatch, NavGrid grid)
    {
        int cellPx = (int)grid.CellSize;
        int border = Math.Max(1, cellPx / 16);

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                NavCell cell = grid.GetCell(x, y);
                Color color = SelectColor(cell);

                Vector2 worldCenter = grid.GridToWorld(x, y);
                int drawX = (int)(worldCenter.X - grid.CellSize * 0.5f) + border;
                int drawY = (int)(worldCenter.Y - grid.CellSize * 0.5f) + border;
                int drawW = cellPx - border * 2;
                int drawH = cellPx - border * 2;

                if (drawW <= 0 || drawH <= 0) continue;

                spriteBatch.Draw(_pixel, new Rectangle(drawX, drawY, drawW, drawH), color);
            }
        }
    }

    private void DrawPath(SpriteBatch spriteBatch, NavGrid grid, NavPath path)
    {
        int dotSize = Math.Max(4, (int)(grid.CellSize * 0.25f));
        int halfDot = dotSize / 2;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2 wp = path.GetWaypoint(i);
            spriteBatch.Draw(_pixel,
                new Rectangle((int)wp.X - halfDot, (int)wp.Y - halfDot, dotSize, dotSize),
                PathColor);
        }
    }

    private Color SelectColor(in NavCell cell)
    {
        if (cell.ObstacleHeight > 0f) return ObstacleColor;
        return cell.IsWalkable ? WalkableColor : BlockedColor;
    }
    #endregion
}
