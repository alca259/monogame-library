namespace Alca.MonoGame.Kernel.Mathematics;

/// <summary>
/// Converts between 2D isometric screen coordinates and logical world coordinates using a 2:1 diamond tile projection.
/// </summary>
public sealed class IsometricHelper
{
    /// <summary>Default tile width in pixels used by <see cref="Default"/>.</summary>
    public const float DefaultTileWidth = 64f;

    /// <summary>Default tile height in pixels used by <see cref="Default"/>.</summary>
    public const float DefaultTileHeight = 32f;

    /// <summary>A shared instance configured for 64×32 tiles.</summary>
    public static readonly IsometricHelper Default = new(DefaultTileWidth, DefaultTileHeight);

    private readonly float _tileWidth;
    private readonly float _tileHeight;
    private readonly float _halfTileWidth;
    private readonly float _halfTileHeight;

    /// <summary>Creates a new <see cref="IsometricHelper"/> with the specified tile dimensions.</summary>
    /// <param name="tileWidth">Width of a single tile in pixels. Defaults to 64.</param>
    /// <param name="tileHeight">Height of a single tile in pixels. Defaults to 32.</param>
    public IsometricHelper(float tileWidth = DefaultTileWidth, float tileHeight = DefaultTileHeight)
    {
        _tileWidth = tileWidth;
        _tileHeight = tileHeight;
        _halfTileWidth = tileWidth * 0.5f;
        _halfTileHeight = tileHeight * 0.5f;
    }

    /// <summary>
    /// Converts a logical world position to a 2D isometric screen position.
    /// Formula: <c>screen.X = (worldPos.X - worldPos.Y) * (tileWidth / 2)</c>,
    /// <c>screen.Y = (worldPos.X + worldPos.Y) * (tileHeight / 2)</c>.
    /// </summary>
    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        return new Vector2(
            (worldPos.X - worldPos.Y) * _halfTileWidth,
            (worldPos.X + worldPos.Y) * _halfTileHeight);
    }

    /// <summary>
    /// Converts a 2D isometric screen position back to logical world coordinates.
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return new Vector2(
            screenPos.X / _tileWidth + screenPos.Y / _tileHeight,
            -screenPos.X / _tileWidth + screenPos.Y / _tileHeight);
    }

    /// <summary>
    /// Returns a <see cref="SpriteBatch"/> layer depth value so that entities with a greater world-Y
    /// are drawn on top of entities with a lesser world-Y (painter's algorithm for isometric scenes).
    /// </summary>
    /// <param name="worldY">Entity's Y position in world space.</param>
    /// <param name="worldHeight">Total height of the world in world units (used for normalization).</param>
    /// <returns>A value in [0, 1] where 0 = front and 1 = back.</returns>
    public float DepthFromWorldY(float worldY, float worldHeight)
        => 1f - MathHelper.Clamp(worldY / worldHeight, 0f, 1f);

    /// <summary>
    /// Returns a <see cref="SpriteBatch"/> layer depth from a 2D world position.
    /// Entities with a greater Y coordinate are rendered on top.
    /// </summary>
    /// <param name="worldPos">Entity's position in world space.</param>
    /// <param name="worldSize">Total size of the world in world units.</param>
    public float DepthFromPosition(Vector2 worldPos, Vector2 worldSize)
        => DepthFromWorldY(worldPos.Y, worldSize.Y);
}
