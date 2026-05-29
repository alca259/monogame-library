using Alca.MonoGame.Kernel.ECS;

namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>
/// Renders a <see cref="Texture2D"/> centered at the entity's world-space position, automatically sorting
/// draw order by the entity's Y coordinate so that lower (more south) entities are drawn on top.
/// </summary>
public sealed class YSortRendererBehaviour : GameBehaviour
{
    private readonly Texture2D _texture;
    private readonly Vector2 _origin;

    /// <summary>Gets or sets the tint color applied when rendering. Default is <see cref="Color.White"/>.</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the total world height used to normalize the Y-sort layer depth.
    /// A larger value gives finer depth granularity.
    /// </summary>
    public float WorldHeight { get; set; }

    /// <summary>
    /// Gets or sets an additional Y offset (in world units) added to the entity's Y position before depth calculation.
    /// Useful for sprites whose visual anchor is not at their logical origin.
    /// </summary>
    public int YOffset { get; set; } = 0;

    /// <summary>
    /// Content-relative path to the sprite texture (e.g. <c>Sprites/Player</c>).
    /// Used by the editor and the scene converter.
    /// </summary>
    public string SpritePath { get; set; } = string.Empty;

    /// <summary>
    /// Creates a renderer that sorts draw order by Y position.
    /// </summary>
    /// <param name="texture">The sprite texture to draw.</param>
    /// <param name="worldHeight">
    /// Total height of the world in world units. Used to normalise the layer depth to [0, 1].
    /// Defaults to 2048.
    /// </param>
    public YSortRendererBehaviour(Texture2D texture, float worldHeight = 2048f)
    {
        _texture = texture;
        _origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
        WorldHeight = worldHeight;
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var t = Entity.Transform;
        float layerDepth = 1f - MathHelper.Clamp((t.Position2d.Y + YOffset) / WorldHeight, 0f, 1f);
        spriteBatch.Draw(_texture, t.Position2d, null, Color, t.Rotation2d, _origin, t.Scale2d, SpriteEffects.None, layerDepth);
    }
}
