using Alca.MonoGame.Kernel.Graphics;
using Alca.MonoGame.Kernel.UI.Core;
using Alca.MonoGame.Kernel.UI.Input;

namespace Alca.MonoGame.Kernel.UI.Controls.Specialized;

/// <summary>
/// A scrollable container that clips its visible area using a ScissorRectangle.
/// The ScissorRectangle is always intersected with the current device scissor to support nesting.
/// </summary>
public sealed class ScrollView : UIContainer
{
    private readonly GraphicsDevice _graphicsDevice;

    /// <summary>Created once in the constructor; never recreated in Draw.</summary>
    private readonly RasterizerState _scissorState;

    private Vector2 _scrollOffset;

    /// <summary>Total content size measured by the last layout pass.</summary>
    public Vector2 ContentSize { get; private set; }

    /// <summary>Current scroll position in pixels (X, Y), clamped to valid range.</summary>
    public Vector2 ScrollOffset => _scrollOffset;

    /// <summary>
    /// When set, Measure returns this size so the scroll view acts as a fixed-size clipped viewport.
    /// Children are measured against the fixed width; content taller/wider than this size becomes scrollable.
    /// </summary>
    public Vector2? FixedSize { get; set; }

    /// <summary>1×1 white pixel texture used to draw background and border. Optional.</summary>
    public Texture2D? Pixel { get; set; }

    /// <summary>Background fill color. Transparent by default.</summary>
    public Color BackColor { get; set; } = Color.Transparent;

    /// <summary>Border color drawn around the viewport. Transparent by default.</summary>
    public Color BorderColor { get; set; } = Color.Transparent;

    /// <summary>Creates a ScrollView bound to the given GraphicsDevice for scissor clipping.</summary>
    public ScrollView(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _scissorState = new RasterizerState { ScissorTestEnable = true };
    }

    #region Layout

    /// <inheritdoc/>
    public override void Measure(Vector2 availableSize)
    {
        Vector2 contentAvail = FixedSize.HasValue
            ? new Vector2(FixedSize.Value.X, float.MaxValue)
            : availableSize;

        for (int i = 0; i < Children.Count; i++)
            Children[i].Measure(contentAvail);

        if (FixedSize.HasValue)
        {
            DesiredSize = FixedSize.Value;
            return;
        }

        float maxW = 0f, totalH = 0f;
        for (int i = 0; i < Children.Count; i++)
        {
            Vector2 ds = Children[i].DesiredSize;
            if (ds.X > maxW) maxW = ds.X;
            totalH += ds.Y;
        }
        DesiredSize = new Vector2(maxW, totalH);
    }

    /// <inheritdoc/>
    public override void Arrange(Rectangle finalBounds)
    {
        Bounds = finalBounds;

        int cursor = finalBounds.Y - (int)_scrollOffset.Y;
        float contentWidth = 0f;

        for (int i = 0; i < ChildrenReadOnly.Count; i++)
        {
            Vector2 ds = ChildrenReadOnly[i].DesiredSize;
            int childH = (int)ds.Y;
            // Give children their natural width when wider than the viewport (enables H scroll)
            int childW = Math.Max((int)ds.X, finalBounds.Width);
            if (ds.X > contentWidth) contentWidth = ds.X;

            ChildrenReadOnly[i].Arrange(new Rectangle(
                finalBounds.X - (int)_scrollOffset.X, cursor, childW, childH));
            cursor += childH;
        }

        float contentHeight = cursor - (finalBounds.Y - (int)_scrollOffset.Y);
        ContentSize = new Vector2(MathF.Max(contentWidth, finalBounds.Width), contentHeight);
        ClampScrollOffset();
    }

    #endregion

    #region Update

    /// <summary>Pixels scrolled per mouse-wheel notch (~120 raw units). Default 40.</summary>
    public int ScrollSpeed { get; set; } = 40;

    /// <inheritdoc/>
    public override void Update(GameTime gameTime)
    {
        if (!IsEnabled) return;

        var input = UIInputContext.Current!;

        int rawDelta = -input.PointerScrollDelta;
        if (rawDelta != 0 && input.PointerPosition is not null && Bounds.Contains(input.PointerPosition.Value))
        {
            float sign = rawDelta > 0 ? 1f : -1f;
            ScrollBy(new Vector2(0f, sign * ScrollSpeed));
        }

        base.Update(gameTime);
    }

    /// <summary>Scrolls by the given delta in pixels, clamped to content bounds.</summary>
    public void ScrollBy(Vector2 delta)
    {
        _scrollOffset += delta;
        ClampScrollOffset();
        Invalidate();
    }

    private void ClampScrollOffset()
    {
        float maxY = MathF.Max(0f, ContentSize.Y - Bounds.Height);
        float maxX = MathF.Max(0f, ContentSize.X - Bounds.Width);
        _scrollOffset = new Vector2(
            MathHelper.Clamp(_scrollOffset.X, 0f, maxX),
            MathHelper.Clamp(_scrollOffset.Y, 0f, maxY));
    }

    #endregion

    #region Draw

    /// <inheritdoc/>
    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!IsVisible) return;

        float opacity = EffectiveOpacity;

        // Background and border drawn outside the scissor region (no clipping needed).
        if (Pixel is not null)
        {
            if (BackColor.A > 0)
                spriteBatch.Draw(Pixel, Bounds, BackColor * opacity);
            if (BorderColor.A > 0)
                DrawHelper.DrawBorder(Pixel, spriteBatch, Bounds, BorderColor * opacity, 1);
        }

        // End current batch, switch to scissor, draw children, restore.
        spriteBatch.End();

        Rectangle savedScissor = _graphicsDevice.ScissorRectangle;
        Rectangle clip = Rectangle.Intersect(Bounds, savedScissor);
        _graphicsDevice.ScissorRectangle = clip;

        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.LinearClamp, null, _scissorState);

        for (int i = 0; i < ChildrenReadOnly.Count; i++)
        {
            if (ChildrenReadOnly[i].IsVisible)
                ChildrenReadOnly[i].Draw(spriteBatch);
        }

        spriteBatch.End();

        _graphicsDevice.ScissorRectangle = savedScissor;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.LinearClamp, null, null);
    }

    #endregion
}
