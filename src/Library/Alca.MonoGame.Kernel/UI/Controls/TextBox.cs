namespace Alca.MonoGame.Kernel.UI.Controls;

/// <summary>Single-line plain-text input field. Accepts all printable characters.</summary>
public sealed class TextBox : TextBoxBase
{
    /// <summary>Creates a new TextBox.</summary>
    /// <param name="font">Font for rendering. May be null.</param>
    /// <param name="pixel">1×1 pixel texture. May be null.</param>
    /// <param name="window">GameWindow for TextInput subscription. Null disables IME input.</param>
    public TextBox(SpriteFont? font, Texture2D? pixel, GameWindow? window)
        : base(font, pixel, window) { }

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        int height = (_font?.LineSpacing ?? 16) + 10;
        DesiredSize = new Vector2(availableSize.X, height);
    }

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;
        DrawSingleLine(spriteBatch, Text);
    }
}
