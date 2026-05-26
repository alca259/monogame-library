namespace Alca.MonoGame.Kernel.ECS;

/// <summary>Renders a Texture2D centered at the entity's world-space transform each frame.</summary>
public sealed class SpriteRendererBehaviour : GameBehaviour
{
    private readonly Texture2D _texture;
    private readonly Vector2 _origin;

    /// <summary>Gets or sets the tint color applied when rendering. Default is <see cref="Color.White"/> (no tint).</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>Gets or sets the draw layer depth in the range [0, 1]. Default is 0 (front).</summary>
    public float LayerDepth { get; set; } = 0f;

    /// <summary>
    /// Content-relative path to the sprite texture (e.g. <c>Sprites/Player</c>).
    /// Used by the editor to preview sprites and by the scene converter when loading at runtime.
    /// </summary>
    public string SpritePath { get; set; } = string.Empty;

    /// <summary>Creates a renderer that draws <paramref name="texture"/> centered at the entity's world-space position.</summary>
    public SpriteRendererBehaviour(Texture2D texture)
    {
        _texture = texture;
        _origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var t = Entity.Transform;
        spriteBatch.Draw(_texture, t.Position2d, null, Color, t.Rotation2d, _origin, t.Scale2d, SpriteEffects.None, LayerDepth);
    }
}
