namespace Alca.MonoGame.Kernel.UI.Core;

/// <summary>Base class for all UI elements in the visual tree.</summary>
public abstract class UIElement
{
    /// <summary>Unique identifier for this element.</summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>True when the element needs a new layout pass; reset by Arrange.</summary>
    public bool IsLayoutDirty { get; private set; } = true;

    /// <summary>Absolute screen rectangle; set by the layout Arrange pass.</summary>
    public Rectangle Bounds { get; internal set; }

    /// <summary>Desired size computed by the Measure pass.</summary>
    public Vector2 DesiredSize { get; protected set; }

    /// <summary>Controls visibility; invisible elements skip Draw and hit testing.</summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>Controls interactability; disabled elements skip Update and input.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Opacity 0–1, multiplied when drawing. 1 = fully opaque.</summary>
    public float Opacity { get; set; } = 1f;

    /// <summary>Reference to the parent element; set internally by UIContainer.</summary>
    public UIElement? Parent { get; internal set; }

    /// <summary>Effective opacity factoring in the full parent chain.</summary>
    public float EffectiveOpacity
    {
        get
        {
            float o = Opacity;
            UIElement? p = Parent;
            while (p is not null) { o *= p.Opacity; p = p.Parent; }
            return o;
        }
    }

    /// <summary>Marks this element and all ancestors as needing a layout pass.</summary>
    public virtual void Invalidate()
    {
        IsLayoutDirty = true;
        Parent?.Invalidate();
    }

    /// <summary>Computes <see cref="DesiredSize"/> given the available space.</summary>
    public virtual void Measure(Vector2 availableSize)
    {
        DesiredSize = Vector2.Zero;
    }

    /// <summary>Sets <see cref="Bounds"/> and performs final placement.</summary>
    public virtual void Arrange(Rectangle finalBounds)
    {
        Bounds = finalBounds;
        IsLayoutDirty = false;
    }

    /// <summary>Called every frame when IsEnabled is true.</summary>
    public virtual void Update(GameTime gameTime) { }

    /// <summary>Called every frame when IsVisible is true. Draw only this element; children are handled by UIContainer.</summary>
    public virtual void Draw(SpriteBatch spriteBatch) { }
}
