using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Overlays;

namespace Alca.MonoGame.Kernel.UI.Controls.Display;

/// <summary>
/// A floating tooltip that displays text near an anchor position with screen-edge clamping.
/// Register it with <see cref="UIOverlayManager"/> and call <see cref="Show"/> / <see cref="Hide"/> to control visibility.
/// </summary>
public sealed class Tooltip : UIElement
{
    private const int PaddingH = 6;
    private const int PaddingV = 4;

    private SpriteFont? _font;
    private string _text = string.Empty;
    private Vector2 _measuredSize;

    /// <summary>Tooltips start hidden; they become visible only when <see cref="Show"/> is called.</summary>
    public Tooltip() => IsVisible = false;

    /// <summary>Text displayed inside the tooltip bubble.</summary>
    public string Text
    {
        get => _text;
        set
        {
            if (_text == value) return;
            _text = value;
            if (_font is not null)
                _measuredSize = _font.MeasureString(_text);
            Invalidate();
        }
    }

    /// <summary>Font used to render the tooltip text.</summary>
    public SpriteFont? Font
    {
        get => _font;
        set
        {
            if (ReferenceEquals(_font, value)) return;
            _font = value;
            if (_font is not null && _text.Length > 0)
                _measuredSize = _font.MeasureString(_text);
            Invalidate();
        }
    }

    /// <summary>Background fill color of the tooltip bubble.</summary>
    public Color BackgroundColor { get; set; } = new Color(20, 20, 20, 220);

    /// <summary>Text color.</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>Pixel texture used to draw the background rectangle.</summary>
    public Texture2D? Pixel { get; set; }

    /// <summary>
    /// The bounds of the screen used for edge-clamping.
    /// Should be set to the viewport rectangle before calling <see cref="Show"/>.
    /// </summary>
    public Rectangle ScreenBounds { get; set; } = new Rectangle(0, 0, 1920, 1080);

    #region Measure / Arrange

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        if (_font is null || _text.Length == 0)
        {
            DesiredSize = Vector2.Zero;
            return;
        }

        _measuredSize = _font.MeasureString(_text);
        DesiredSize = new Vector2(_measuredSize.X + PaddingH * 2, _measuredSize.Y + PaddingV * 2);
    }

    #endregion

    #region Show / Hide

    /// <summary>Positions the tooltip near <paramref name="anchorPos"/> and makes it visible.</summary>
    public void Show(Vector2 anchorPos)
    {
        if (_font is null) return;

        if (_measuredSize == Vector2.Zero && _text.Length > 0)
            _measuredSize = _font.MeasureString(_text);

        int w = (int)_measuredSize.X + PaddingH * 2;
        int h = (int)_measuredSize.Y + PaddingV * 2;

        Bounds = ComputeClampedBounds(anchorPos, w, h, ScreenBounds);
        IsVisible = true;
    }

    /// <summary>Hides the tooltip.</summary>
    public void Hide()
    {
        IsVisible = false;
    }

    /// <summary>Computes the clamped tooltip rectangle given an anchor position, size, and screen bounds.</summary>
    public static Rectangle ComputeClampedBounds(Vector2 anchorPos, int width, int height, Rectangle screenBounds)
    {
        int x = (int)anchorPos.X;
        int y = (int)anchorPos.Y;

        if (x + width > screenBounds.Right) x = screenBounds.Right - width;
        if (y + height > screenBounds.Bottom) y = screenBounds.Bottom - height;
        if (x < screenBounds.Left) x = screenBounds.Left;
        if (y < screenBounds.Top) y = screenBounds.Top;

        return new Rectangle(x, y, width, height);
    }

    #endregion

    #region Draw

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible || _font is null || _text.Length == 0) return;

        float opacity = EffectiveOpacity;

        if (Pixel is not null)
            spriteBatch.Draw(Pixel, Bounds, BackgroundColor * opacity);

        spriteBatch.DrawString(_font, _text,
            new Vector2(Bounds.X + PaddingH, Bounds.Y + PaddingV),
            TextColor * opacity);
    }

    #endregion
}
