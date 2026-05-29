using Alca.MonoGame.Kernel.ECS;
using Alca.MonoGame.Kernel.Graphics.Camera;

namespace Alca.MonoGame.Kernel.Graphics.Sprites;

/// <summary>
/// Renders a <see cref="Texture2D"/> that always faces the camera by cancelling the camera's rotation.
/// Useful in 2.5D or top-down scenes where world objects should remain upright regardless of camera angle.
/// </summary>
public sealed class BillboardSpriteBehaviour : GameBehaviour
{
    private readonly Texture2D _texture;
    private readonly Camera2D _camera;
    private readonly Vector2 _origin;

    /// <summary>Gets or sets the tint color applied when rendering. Default is <see cref="Color.White"/>.</summary>
    public Color Color { get; set; } = Color.White;

    /// <summary>Gets or sets the draw layer depth in [0, 1]. Default is 0 (front).</summary>
    public float LayerDepth { get; set; } = 0f;

    /// <summary>
    /// Creates a new <see cref="BillboardSpriteBehaviour"/>.
    /// </summary>
    /// <param name="texture">The sprite texture to render.</param>
    /// <param name="camera">The active <see cref="Camera2D"/> whose rotation is cancelled to keep the sprite upright.</param>
    public BillboardSpriteBehaviour(Texture2D texture, Camera2D camera)
    {
        _texture = texture;
        _camera = camera;
        _origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
    }

    /// <inheritdoc/>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var t = Entity.Transform;
        // Cancel the camera rotation so the billboard always faces the screen.
        float drawRotation = t.Rotation2d - _camera.Rotation;
        spriteBatch.Draw(_texture, t.Position2d, null, Color, drawRotation, _origin, t.Scale2d, SpriteEffects.None, LayerDepth);
    }
}
